using System.Text.Json;
using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Web.Models.Incidents;

namespace EmergencyResponse.UnitTests.Web;

public sealed class EvidenceAnalysisNoticeTests
{
    [Fact]
    public void DisabledApiCapability_DoesNotPromiseAnalysis()
    {
        var response = JsonSerializer.Deserialize<IncidentResponse>("""{"visualAnalysisEnabled":false}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var notice = EvidenceAnalysisNotice.For(response.VisualAnalysisEnabled);
        Assert.Contains("disabled", notice);
        Assert.Contains("evidence only", notice);
        Assert.Contains("no AI analysis will run", notice);
        Assert.DoesNotContain("will automatically", notice);
        Assert.DoesNotContain("queued", notice);
    }

    [Fact]
    public void MissingCapability_FromOlderApi_DoesNotAssumeEnabled()
    {
        var response = JsonSerializer.Deserialize<IncidentResponse>("{}")!;
        Assert.Null(response.VisualAnalysisEnabled);
        var notice = EvidenceAnalysisNotice.For(response.VisualAnalysisEnabled);
        Assert.Contains("has not been confirmed", notice);
        Assert.DoesNotContain("is enabled", notice);
    }

    [Fact]
    public void EnabledCapability_DescribesQueueingAndReview_NotGuaranteedResults()
    {
        var notice = EvidenceAnalysisNotice.For(true);
        Assert.Contains("can be queued", notice);
        Assert.Contains("availability and human review", notice);
        Assert.DoesNotContain("will automatically", notice);
    }
}
