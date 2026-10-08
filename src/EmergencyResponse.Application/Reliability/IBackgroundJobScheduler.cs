namespace EmergencyResponse.Application.Reliability;

/// <summary>Adds job requests to the current database unit of work; it does not save or publish them.</summary>
public interface IBackgroundJobScheduler
{
    Guid ScheduleIncidentAI(Guid incidentId);
    Guid ScheduleDuplicateDetection(Guid incidentId);
    Guid ScheduleExternalContext(Guid incidentId);
    Guid ScheduleVisualAnalysis(Guid incidentId, Guid mediaId);
}
