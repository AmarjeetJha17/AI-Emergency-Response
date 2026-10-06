using System.Net;
using System.Net.Http.Json;
using EmergencyResponse.Application.Copilot;
using EmergencyResponse.Application.Copilot.Actions;

namespace EmergencyResponse.Web.Services;

public sealed class CopilotApiClient : ICopilotApiClient
{
    private readonly HttpClient _httpClient;

    public CopilotApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CopilotResponse?> QueryAsync(
        CopilotQuery query,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/copilot/query",
            query,
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CopilotResponse>(
            cancellationToken);
    }

    public async Task<CopilotActionExecutionResult> ApproveActionAsync(
        Guid proposalId,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/copilot/actions/{proposalId}/approve",
            new ApproveCopilotActionRequest(comment),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CopilotActionExecutionResult>(
            cancellationToken)
            ?? throw new InvalidOperationException("The action result was empty.");
    }

    public async Task<CopilotActionProposalResult?> RejectActionAsync(
        Guid proposalId,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/copilot/actions/{proposalId}/reject",
            new RejectCopilotActionRequest(comment),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CopilotActionProposalResult>(
            cancellationToken);
    }
}
