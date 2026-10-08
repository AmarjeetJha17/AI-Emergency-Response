using EmergencyResponse.Application.Predictive.Training;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyResponse.IntegrationTests.Predictive;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Integration)]
[Collection("SQL Server integration database")]
public sealed class PredictionSnapshotTests
    : IClassFixture<EmergencyResponseWebApplicationFactory>
{
    private readonly EmergencyResponseWebApplicationFactory _factory;

    public PredictionSnapshotTests(EmergencyResponseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LabelGenerator_UsesTheFirstEscalationWithinSixHours()
    {
        var incidentId = Guid.NewGuid();
        var snapshotAt = DateTime.UtcNow.AddHours(-8);
        var escalationId = Guid.NewGuid();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var incidentType = new IncidentType
            {
                Id = Guid.NewGuid(),
                Name = "Predictive Test",
                Code = $"PRED-{Guid.NewGuid():N}"[..12]
            };
            dbContext.IncidentTypes.Add(incidentType);
            dbContext.Incidents.Add(new Incident
            {
                Id = incidentId,
                IncidentNumber = $"PRED-{Guid.NewGuid():N}"[..16],
                Title = "Predictive test incident",
                Description = "Controlled historical label test",
                IncidentTypeId = incidentType.Id,
                ReportedAtUtc = snapshotAt.AddMinutes(-10)
            });
            dbContext.IncidentEscalations.Add(new IncidentEscalation
            {
                Id = escalationId,
                IncidentId = incidentId,
                Type = EscalationType.ManualEscalation,
                Level = EscalationLevel.High,
                Reason = "Controlled test",
                CreatedAtUtc = snapshotAt.AddMinutes(30)
            });
            await dbContext.SaveChangesAsync();
            var persistedEscalation = await dbContext.IncidentEscalations
                .SingleAsync(x => x.Id == escalationId);
            persistedEscalation.CreatedAtUtc = snapshotAt.AddMinutes(30);
            await dbContext.SaveChangesAsync();
        }

        await using var labelScope = _factory.Services.CreateAsyncScope();
        var generator = labelScope.ServiceProvider
            .GetRequiredService<IEscalationLabelGenerator>();
        var result = await generator.GenerateAsync(incidentId, snapshotAt);

        Assert.True(result.Label);
        Assert.Equal(escalationId, result.OutcomeEscalationId);
        Assert.Equal(snapshotAt.AddMinutes(30), result.OutcomeAtUtc);
    }
}
