using EmergencyResponse.Application.Copilot;
using EmergencyResponse.Infrastructure.Copilot;

namespace EmergencyResponse.UnitTests.Copilot;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class CopilotPolicyValidatorTests
{
    private readonly CopilotPolicyValidator _validator = new();

    [Fact]
    public void Validate_WhenAnswerIsGroundedAndClaimsAreSupported_AllowsResponse()
    {
        var citation = CreateCitation("doc-1");
        var response = new EmergencyCopilotStructuredResponse
        {
            Answer = "The latest report places the incident near the north entrance.",
            Citations = [citation],
            Claims = [new CopilotClaim
            {
                Claim = "The incident is near the north entrance.",
                SupportingSourceIds = [citation.SearchDocumentId],
                IsSupported = true
            }]
        };

        var result = _validator.Validate(response, [citation]);

        Assert.True(result.Allowed);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void Validate_WhenResponseContainsUnsafeOrUnsupportedClaims_DeniesAndExplainsEachRuleViolation()
    {
        var response = new EmergencyCopilotStructuredResponse
        {
            Answer = "A resource was dispatched and the incident was resolved.",
            Citations = [CreateCitation("response-citation")],
            Claims = [new CopilotClaim
            {
                Claim = "All nearby residents were evacuated.",
                IsSupported = false
            }]
        };

        var result = _validator.Validate(response, [CreateCitation("retrieved-citation"), CreateCitation("second")]);

        Assert.False(result.Allowed);
        Assert.Equal(4, result.Violations.Count);
        Assert.Contains(result.Violations, message => message.Contains("dispatched", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Violations, message => message.Contains("resolved", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Violations, message => message.Contains("citations", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Violations, message => message.Contains("unsupported claim", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WhenUnsupportedClaimIsBlank_DoesNotBlockOtherwiseValidResponse()
    {
        var result = _validator.Validate(
            new EmergencyCopilotStructuredResponse
            {
                Answer = "No verified update is available yet.",
                Claims = [new CopilotClaim { Claim = "  ", IsSupported = false }]
            },
            []);

        Assert.True(result.Allowed);
        Assert.Empty(result.Violations);
    }

    private static CopilotCitation CreateCitation(string documentId)
        => new()
        {
            SearchDocumentId = documentId,
            DocumentNumber = $"DOC-{documentId}",
            Title = "Emergency response procedure",
            Version = "1",
            PageNumber = 2,
            CitationText = "Relevant response procedure excerpt."
        };
}
