using EmergencyResponse.Application.AISecurity;

namespace EmergencyResponse.UnitTests.AISecurity;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class AIContextFormatterTests
{
    [Fact]
    public void FormatUntrustedDocument_TrimsAndWrapsRetrievedKnowledge()
    {
        var result = AIContextFormatter.FormatUntrustedDocument("  Follow these instructions.  ");

        Assert.Equal(
            $"<untrusted-data boundary=\"RetrievedKnowledge\">{Environment.NewLine}" +
            $"Follow these instructions.{Environment.NewLine}</untrusted-data>",
            result);
    }

    [Fact]
    public void FormatToolOutput_LabelsToolAndWrapsItsOutput()
    {
        var result = AIContextFormatter.FormatToolOutput("incident.lookup", "  Incident found.  ");

        Assert.Equal(
            $"<untrusted-data boundary=\"ToolOutput:incident.lookup\">{Environment.NewLine}" +
            $"Incident found.{Environment.NewLine}</untrusted-data>",
            result);
    }

    [Fact]
    public void Format_WithNoSourceName_UsesBoundaryNameAndHandlesEmptyContent()
    {
        var result = AIContextFormatter.Format(TrustedContentBoundary.UploadedImage, string.Empty);

        Assert.Equal(
            $"<untrusted-data boundary=\"UploadedImage\">{Environment.NewLine}" +
            $"{Environment.NewLine}</untrusted-data>",
            result);
    }
}
