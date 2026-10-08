using EmergencyResponse.Application.Common.Configuration;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Domain.Notifications;
using EmergencyResponse.Infrastructure.BackgroundJobs;
using EmergencyResponse.Infrastructure.Identity;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.UnitTests.Security;

public sealed class DataRetentionTests
{
    [Fact]
    public async Task RunOnce_ArchivesOldConversations_DeletesOldNotifications_AndKeepsOperationalData()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        await using var provider = services.BuildServiceProvider();
        await using (var setup = provider.CreateAsyncScope())
        {
            var db = setup.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.EnsureCreatedAsync();
            db.CopilotConversations.Add(new CopilotConversation
            {
                Id = Guid.NewGuid(),
                UserId = "user-1",
                LastActivityAtUtc = DateTime.UtcNow.AddDays(-100)
            });
            db.CopilotConversations.Add(new CopilotConversation
            {
                Id = Guid.NewGuid(),
                UserId = "user-2",
                LastActivityAtUtc = DateTime.UtcNow.AddDays(-10)
            });
            db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                Title = "Old",
                Message = "Expired by age",
                Type = NotificationType.IncidentCreated,
                Priority = NotificationPriority.Normal
            });
            var incident = new Incident
            {
                Id = Guid.NewGuid(),
                IncidentNumber = "INC-RETENTION-1",
                Title = "Retain incident",
                Description = "Operational record",
                IncidentTypeId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ReportedAtUtc = DateTime.UtcNow,
                Status = IncidentStatus.Submitted,
                Severity = SeverityLevel.Moderate,
                Priority = PriorityLevel.Normal
            };
            db.IncidentTypes.Add(new IncidentType
            {
                Id = incident.IncidentTypeId,
                Name = "General",
                Code = "RETENTION-TEST"
            });
            db.Incidents.Add(incident);
            await db.SaveChangesAsync();
            await db.Notifications.ExecuteUpdateAsync(s => s
                .SetProperty(x => x.CreatedAtUtc, DateTime.UtcNow.AddDays(-31)));
        }

        var worker = new DataRetentionBackgroundWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new RetentionOptions()),
            NullLogger<DataRetentionBackgroundWorker>.Instance);
        await worker.RunOnceAsync();

        await using var verifyScope = provider.CreateAsyncScope();
        var verify = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await verify.CopilotConversations.CountAsync(x => x.ArchivedAtUtc != null));
        Assert.Equal(1, await verify.CopilotConversations.CountAsync(x => x.ArchivedAtUtc == null));
        Assert.Empty(await verify.Notifications.ToListAsync());
        Assert.Single(await verify.Incidents.ToListAsync());
        var retentionAudit = await verify.AuditLogs.SingleAsync();
        Assert.Equal("SEC_DATA_RETENTION_EXECUTED", retentionAudit.Action);
        Assert.Contains("archivedConversations", retentionAudit.NewValues);
    }

    [Fact]
    public void EntityDefaults_UseConservativeClassification()
    {
        Assert.Equal(DataClassification.Restricted, new ApplicationUser().DataClassification);
        Assert.Equal(DataClassification.Confidential, new Incident().DataClassification);
        Assert.Equal(DataClassification.Confidential, new IncidentMedia().DataClassification);
        Assert.Equal(DataClassification.Confidential, new AIAnalysis().DataClassification);
        Assert.Equal(DataClassification.Confidential, new CopilotConversation().DataClassification);
        Assert.Equal(DataClassification.Internal, new KnowledgeDocument().DataClassification);
        Assert.Equal(DataClassification.Restricted, new AuditLog().DataClassification);
    }
}
