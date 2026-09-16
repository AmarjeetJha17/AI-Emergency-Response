namespace EmergencyResponse.Web.Models.Incidents;

public sealed class ReviewDuplicateIncidentRequest
{
    public bool ConfirmedDuplicate { get; set; }

    public string? ReviewDecision { get; set; }

    public Guid? CanonicalIncidentId { get; set; }
}