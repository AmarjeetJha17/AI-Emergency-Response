using EmergencyResponse.Api.Hubs;
using EmergencyResponse.Api.Realtime;
using EmergencyResponse.Api.Services;
using EmergencyResponse.Application.Events;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace EmergencyResponse.UnitTests.Realtime;

public sealed class IncidentNotificationServiceTests
{
    [Theory]
    [InlineData("reporter-18")]
    [InlineData("")]
    [InlineData(null)]
    public async Task IncidentUpdated_IsScopedToOperatorsAndActualReporter(string? reporter)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new NotificationTestDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection).Options);
        // Only the projected ownership columns are needed. No operational fixture is seeded.
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE Incidents (Id TEXT PRIMARY KEY, ReporterUserId TEXT)");
        var incidentId = Guid.NewGuid();
        if (reporter is not null)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Incidents (Id, ReporterUserId) VALUES ({incidentId}, {reporter})");
        }
        var notification = new IncidentUpdatedEvent { IncidentId = incidentId, Status = "UnderReview" };
        var clients = new Mock<IHubClients>(MockBehavior.Strict);
        var proxy = new Mock<IClientProxy>(MockBehavior.Strict);
        IReadOnlyList<string>? recipients = null;
        clients.Setup(x => x.Groups(It.IsAny<IReadOnlyList<string>>()))
            .Callback<IReadOnlyList<string>>(groups => recipients = groups.ToArray())
            .Returns(proxy.Object);
        using var cancellation = new CancellationTokenSource();
        proxy.Setup(x => x.SendCoreAsync("IncidentUpdated", It.Is<object?[]>(args =>
                args.Length == 1 && ReferenceEquals(args[0], notification)), cancellation.Token))
            .Returns(Task.CompletedTask);
        var hub = new Mock<IHubContext<IncidentHub>>(MockBehavior.Strict);
        hub.SetupGet(x => x.Clients).Returns(clients.Object);

        await new IncidentNotificationService(hub.Object, db)
            .NotifyIncidentUpdatedAsync(notification, cancellation.Token);

        Assert.NotNull(recipients);
        Assert.Equal(string.IsNullOrWhiteSpace(reporter)
            ? [SignalRGroups.DashboardOperators]
            : new[] { SignalRGroups.DashboardOperators, $"user:{reporter}" }, recipients);
        clients.VerifyGet(x => x.All, Times.Never);
        clients.Verify(x => x.Groups(It.IsAny<IReadOnlyList<string>>()), Times.Once);
        proxy.VerifyAll();
        clients.VerifyNoOtherCalls();
    }

    private sealed class NotificationTestDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Incident>().Ignore(x => x.Embedding);
        }
    }
}
