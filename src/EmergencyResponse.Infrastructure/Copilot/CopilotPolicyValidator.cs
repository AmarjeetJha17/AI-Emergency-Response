using EmergencyResponse.Application.Copilot;

namespace EmergencyResponse.Infrastructure.Copilot;

public sealed class CopilotPolicyValidator : ICopilotPolicyValidator
{
    private static readonly string[] MutationTerms =
    [
        "dispatched", "assigned", "changed severity", "changed priority",
        "acknowledged", "resolved", "evacuate now", "evacuation ordered"
    ];

    public CopilotPolicyValidationResult Validate(
        EmergencyCopilotStructuredResponse response,
        IReadOnlyList<CopilotCitation> validCitations)
    {
        var violations = new List<string>();
        var answer = response.Answer;
        foreach (var term in MutationTerms)
        {
            if (answer.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                violations.Add($"Response contains an unverified mutation claim: {term}.");
            }
        }

        if (response.Citations.Count != validCitations.Count)
        {
            violations.Add("Response contains citations that do not match retrieved context.");
        }

        if (response.Claims.Any(claim =>
                !claim.IsSupported &&
                !string.IsNullOrWhiteSpace(claim.Claim)))
        {
            violations.Add("Response contains an explicitly unsupported claim.");
        }

        return new CopilotPolicyValidationResult
        {
            Allowed = violations.Count == 0,
            Violations = violations
        };
    }
}
