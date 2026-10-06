using EmergencyResponse.Application.Copilot.Tools;
using EmergencyResponse.Application.Knowledge;

namespace EmergencyResponse.Infrastructure.Copilot.Tools;

public sealed class KnowledgeCopilotTools
{
    private readonly IKnowledgeSearchService _search;
    private readonly IKnowledgeAccessContextFactory _accessContextFactory;

    public KnowledgeCopilotTools(
        IKnowledgeSearchService search,
        IKnowledgeAccessContextFactory accessContextFactory)
    {
        _search = search;
        _accessContextFactory = accessContextFactory;
    }

    public async Task<CopilotToolResult<IReadOnlyList<KnowledgeSearchResult>>>
        SearchEmergencyKnowledgeAsync(
            string query,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length > 1000)
        {
            throw new ArgumentException(
                "Knowledge search text must be between 1 and 1000 characters.",
                nameof(query));
        }

        if (!_search.IsAvailable)
        {
            return CopilotToolResult<IReadOnlyList<KnowledgeSearchResult>>.Unavailable(
                "SEARCH_UNAVAILABLE",
                "The authorized emergency knowledge search is unavailable.");
        }

        var access = await _accessContextFactory.CreateAsync(cancellationToken);
        var results = await _search.SearchAsync(
            query.Trim(),
            access,
            cancellationToken);
        return CopilotToolResult<IReadOnlyList<KnowledgeSearchResult>>.Available(
            results.Take(8).ToList());
    }
}
