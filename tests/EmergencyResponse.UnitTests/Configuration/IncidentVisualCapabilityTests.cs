using EmergencyResponse.Api.Controllers;
using EmergencyResponse.Application.AI.DuplicateDetection;
using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.AI.VisualAnalysis;
using EmergencyResponse.Application.Common.Configuration;
using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Media;
using EmergencyResponse.Application.Predictive;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace EmergencyResponse.UnitTests.Configuration;

public sealed class IncidentVisualCapabilityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DetailsCapability_ComesFromApiOptions_NotStoredIncident(bool enabled)
    {
        var id = Guid.NewGuid();
        var service = new Mock<IIncidentService>();
        service.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncidentResponse { Id = id, VisualAnalysisEnabled = !enabled });
        var controller = new IncidentsController(service.Object,
            Mock.Of<IDuplicateIncidentService>(), Mock.Of<IIncidentConsolidationService>(), null!,
            Mock.Of<ICurrentUserService>(), Mock.Of<IIncidentNotificationService>(),
            Mock.Of<IIncidentMediaService>(), Mock.Of<IVisualAnalysisQueue>(), Mock.Of<IIncidentMediaStorage>(),
            predictionService: Mock.Of<IEscalationRiskPredictionService>(),
            aiOptions: Options.Create(new AIOptions { Enabled = enabled }));

        var result = await controller.GetById(id, default);
        var payload = Assert.IsType<IncidentResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(enabled, payload.VisualAnalysisEnabled);
    }
}
