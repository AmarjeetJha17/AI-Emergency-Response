using EmergencyResponse.Application.AI.Jobs;

namespace EmergencyResponse.Application.AI.Interfaces;

public interface IIncidentAIJobProcessor
{
    Task ProcessAsync(
        IncidentAIJob job,
        CancellationToken cancellationToken = default);
}