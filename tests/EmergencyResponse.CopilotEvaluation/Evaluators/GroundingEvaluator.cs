namespace EmergencyResponse.CopilotEvaluation.Evaluators;

public sealed class GroundingEvaluator
{
    public GroundingScore Evaluate(
        IReadOnlySet<string> requiredFacts,
        IReadOnlySet<string> supportedFacts,
        IReadOnlySet<string> assertedFacts)
    {
        var groundedAssertions = assertedFacts.Intersect(supportedFacts, StringComparer.OrdinalIgnoreCase).Count();
        var unsupportedAssertions = assertedFacts.Except(supportedFacts, StringComparer.OrdinalIgnoreCase).Count();
        var coveredRequired = requiredFacts.Intersect(assertedFacts, StringComparer.OrdinalIgnoreCase).Count();
        return new GroundingScore
        {
            Groundedness = assertedFacts.Count == 0 ? (requiredFacts.Count == 0 ? 1 : 0) : (double)groundedAssertions / assertedFacts.Count,
            UnsupportedClaimRate = assertedFacts.Count == 0 ? 0 : (double)unsupportedAssertions / assertedFacts.Count,
            RequiredFactCoverage = requiredFacts.Count == 0 ? 1 : (double)coveredRequired / requiredFacts.Count
        };
    }
}

public sealed class GroundingScore
{
    public double Groundedness { get; init; }
    public double UnsupportedClaimRate { get; init; }
    public double RequiredFactCoverage { get; init; }
}
