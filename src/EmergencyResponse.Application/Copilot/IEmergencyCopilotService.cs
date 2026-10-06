namespace EmergencyResponse.Application.Copilot;

public interface IEmergencyCopilotService
{
    Task<CopilotResponse> QueryAsync(
        CopilotQuery query,
        CancellationToken cancellationToken = default);
}
