using EmergencyResponse.Application.Knowledge;
using EmergencyResponse.Domain.Knowledge;

namespace EmergencyResponse.UnitTests.Knowledge;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class KnowledgeGroundingTests
{
    [Fact]
    public void Insufficient_evidence_has_explicit_status()
    {
        var response = new EmergencyResponse.Application.Copilot.CopilotResponse
        {
            Answer = "I couldn't find enough information.",
            GroundingStatus = GroundingStatus.InsufficientEvidence,
            SourcesSufficient = false
        };

        Assert.Equal(
            GroundingStatus.InsufficientEvidence,
            response.GroundingStatus);
        Assert.False(response.SourcesSufficient);
    }
}
