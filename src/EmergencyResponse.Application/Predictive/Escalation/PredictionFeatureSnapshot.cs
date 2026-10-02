using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace EmergencyResponse.Application.Predictive.Escalation;

public sealed class PredictionFeatureSnapshot
{
    public Guid IncidentId { get; init; }

    public DateTime SnapshotAtUtc { get; init; }

    public string FeatureHash { get; init; } = string.Empty;

    public EscalationPredictionFeatures Features { get; init; } = new();

    public static string ComputeFeatureHash(EscalationPredictionFeatures f)
    {
        // Canonical string representation of the 15 features
        var canonical = string.Create(CultureInfo.InvariantCulture,
            $"{f.SeverityScore:F2}|{f.PriorityScore:F2}|{f.PeopleAffected:F0}|{f.VehiclesAffected:F0}|" +
            $"{f.Casualties:F0}|{f.ImmediateDanger:F0}|{f.PeopleTrapped:F0}|{f.CriticalInfrastructureAffected:F0}|" +
            $"{f.InfrastructureDamage:F0}|{f.RoadBlocked:F0}|{f.EscalationCount:F0}|{f.DuplicateCount:F0}|" +
            $"{f.RelatedIncidentCount:F0}|{f.ResourceShortage:F0}|{f.IncidentAgeMinutes:F1}");

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
