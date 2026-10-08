using EmergencyResponse.Application.AISecurity;

namespace EmergencyResponse.Infrastructure.AI;

public sealed class DeterministicPromptShieldClient : IPromptShieldClient
{
    public Task<PromptShieldResult> AnalyzeAsync(
        string userPrompt,
        IReadOnlyCollection<string> documents,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var promptSignals = FindSignals(userPrompt);
        var documentSignals = documents
            .SelectMany(FindSignals)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return Task.FromResult(new PromptShieldResult
        {
            IsAvailable = true,
            PromptAttackDetected = promptSignals.Count > 0,
            DocumentAttackDetected = documentSignals.Length > 0,
            AttackDetected = promptSignals.Count > 0 || documentSignals.Length > 0,
            Signals = promptSignals
                .Select(x => $"prompt:{x}")
                .Concat(documentSignals.Select(x => $"document:{x}"))
                .ToArray()
        });
    }

    private static IReadOnlyList<string> FindSignals(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        var normalized = string.Join(
            ' ',
            input.Trim().ToLowerInvariant()
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return PromptInjectionPatterns.HighRiskPatterns
            .Where(normalized.Contains)
            .ToArray();
    }
}
