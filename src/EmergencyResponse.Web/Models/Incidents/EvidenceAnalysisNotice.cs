namespace EmergencyResponse.Web.Models.Incidents;

public static class EvidenceAnalysisNotice
{
    public static string For(bool? enabled) => enabled switch
    {
        true => "AI analysis is enabled. Accepted uploads can be queued for analysis; results remain subject to availability and human review.",
        false => "AI analysis is disabled for this deployment. Accepted uploads are stored as evidence only; no AI analysis will run.",
        null => "AI analysis availability has not been confirmed. Uploading evidence does not confirm that analysis will run."
    };
}
