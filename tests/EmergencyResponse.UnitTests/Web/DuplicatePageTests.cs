using System.Net;
using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Web.Controllers;
using EmergencyResponse.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EmergencyResponse.UnitTests.Web;

public sealed class DuplicatePageTests
{
    [Theory]
    [InlineData(DuplicateDetectionStatus.NotStarted)]
    [InlineData(DuplicateDetectionStatus.Processing)]
    [InlineData(DuplicateDetectionStatus.Failed)]
    public async Task UnassessedIncident_ShowsUnavailableWithoutComputingOrClaimingMatches(DuplicateDetectionStatus status)
    {
        var id = Guid.NewGuid();
        var api = new Mock<IIncidentApiClient>(MockBehavior.Strict);
        api.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncidentResponse { Id = id, DuplicateDetectionStatus = status });
        var controller = Create(api);
        var result = Assert.IsType<ViewResult>(await controller.Duplicates(id, default));
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<DuplicateIncidentCandidate>>(result.Model));
        Assert.Equal(true, controller.ViewData["DuplicateAssessmentUnavailable"]);
        Assert.Equal(status, controller.ViewData["DuplicateDetectionStatus"]);
        api.Verify(x => x.GetDuplicatesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompletedAssessment_ReturnsActualCandidatesAndReviewIdentity()
    {
        var id = Guid.NewGuid();
        var candidate = new DuplicateIncidentCandidate { MatchId = Guid.NewGuid(), IncidentId = Guid.NewGuid(), DuplicateScore = .81, RequiresReview = true };
        var api = new Mock<IIncidentApiClient>(MockBehavior.Strict);
        api.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(new IncidentResponse { Id = id, DuplicateDetectionStatus = DuplicateDetectionStatus.Completed });
        api.Setup(x => x.GetDuplicatesAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { candidate });
        var controller = Create(api);
        var result = Assert.IsType<ViewResult>(await controller.Duplicates(id, default));
        Assert.Same(candidate, Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<DuplicateIncidentCandidate>>(result.Model)));
        Assert.Equal(id, controller.ViewData["SourceIncidentId"]);
        Assert.Null(controller.ViewData["DuplicateAssessmentUnavailable"]);
        api.Verify(x => x.GetDuplicatesAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict, true)]
    [InlineData(HttpStatusCode.Forbidden, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    public async Task HttpErrors_OnlyMissingAssessmentIsShownAsUnavailable(HttpStatusCode status, bool handled)
    {
        var id = Guid.NewGuid();
        var api = new Mock<IIncidentApiClient>(MockBehavior.Strict);
        api.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(new IncidentResponse { Id = id, DuplicateDetectionStatus = DuplicateDetectionStatus.Completed });
        api.Setup(x => x.GetDuplicatesAsync(id, It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("Assessment request failed", null, status));
        var controller = Create(api);
        if (handled)
        {
            var view = Assert.IsType<ViewResult>(await controller.Duplicates(id, default));
            Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<DuplicateIncidentCandidate>>(view.Model));
            Assert.Equal(true, controller.ViewData["DuplicateAssessmentUnavailable"]);
        }
        else await Assert.ThrowsAsync<HttpRequestException>(() => controller.Duplicates(id, default));
    }

    [Fact]
    public async Task MissingIncident_ReturnsNotFoundWithoutQueryingCandidates()
    {
        var id = Guid.NewGuid();
        var api = new Mock<IIncidentApiClient>(MockBehavior.Strict);
        api.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((IncidentResponse?)null);
        Assert.IsType<NotFoundResult>(await Create(api).Duplicates(id, default));
        api.Verify(x => x.GetDuplicatesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static IncidentsController Create(Mock<IIncidentApiClient> api) => new(api.Object,
        Mock.Of<IIncidentTypeApiClient>(), Mock.Of<ISituationApiClient>());
}
