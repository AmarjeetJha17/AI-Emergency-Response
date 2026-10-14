using System.Security.Cryptography;
using System.Text;
using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Domain.Notifications;

namespace EmergencyResponse.Infrastructure.Persistence.Seed;

public sealed record PortfolioDemoSeedOptions(
    string EnvironmentName, string ReporterUserId, string OperatorUserId);

public sealed record PortfolioDemoSnapshot(
    bool Created, IReadOnlyList<Guid> IncidentIds, IReadOnlyList<Guid> ResourceIds,
    IReadOnlyList<Guid> EscalationIds, IReadOnlyList<Guid> SituationIds,
    IReadOnlyList<Guid> NotificationIds);

public sealed record PortfolioDemoCleanupResult(bool Applied, IReadOnlyDictionary<string, int> RecordCounts);

// A definition used only by the existing DatabaseSeeder's explicit demo methods.
// These are synthetic reported observations and manual exercise records, never
// model outputs, dispatches, confirmed duplicates, or generated assessments.
internal static class PortfolioDemoDataset
{
    internal const string Key = "DEMO18-v1";
    internal const string OwnerAction = "DEMO18-SeedOwnership";
    internal static Guid ManifestId => Id("manifest");

    internal static Guid Id(string key) => new(SHA256.HashData(
        Encoding.UTF8.GetBytes($"{Key}/{key}"))[..16]);

    internal static List<BaseEntity> Build(
        IReadOnlyDictionary<string, Guid> types, IReadOnlyDictionary<string, Guid> departments,
        string reporterId, string operatorId, DateTime reportedAtUtc)
    {
        var records = new List<BaseEntity>();
        var scenarios = new[]
        {
            ("FLOOD-01", "FLOOD", "Flood with trapped vehicles", "Synthetic witness report: two vehicles are stopped in rising water at DEMO18 Underpass. Two adult occupants are reported; rescue and injuries have not been verified.", 18.520400m, 73.856700m, (int?)2, (int?)2),
            ("FIRE-01", "FIRE", "Apartment fire; occupants uncertain", "Synthetic witness report: smoke is visible at DEMO18 Apartment A. The number of occupants and whether anyone is trapped are unknown. No casualty assessment is available.", 18.535000m, 73.870000m, (int?)null, (int?)null),
            ("ROAD-01", "ROAD_ACCIDENT", "Road collision with a blocked lane", "Synthetic witness report: two vehicles collided at DEMO18 Junction. One lane is blocked. Two people are reported nearby; injuries and casualty information are unconfirmed.", 18.510000m, 73.865000m, (int?)2, (int?)2),
            ("COLLAPSE-01", "BUILDING_COLLAPSE", "Building collapse; casualties uncertain", "Synthetic witness report: part of DEMO18 Building B has collapsed. Occupancy, trapped people and casualty counts are unknown. No fatalities or rescue outcome have been confirmed.", 18.545000m, 73.850000m, (int?)null, (int?)null),
            ("FLOOD-02", "FLOOD", "Flood report near the same underpass", "Synthetic second report near DEMO18 Underpass describes two trapped vehicles and two occupants. It may describe DEMO18-FLOOD-01; this relationship has not been confirmed.", 18.520500m, 73.856800m, (int?)2, (int?)2),
            ("DUPLICATE-01", "FLOOD", "Possible repeat of the trapped-vehicle report", "Synthetic overlapping report of two vehicles and two occupants at DEMO18 Underpass. Intended for the duplicate-detection exercise; no similarity measurements or duplicate decision have been made.", 18.520450m, 73.856750m, (int?)2, (int?)2)
        };
        foreach (var (key, type, title, description, lat, lon, people, vehicles) in scenarios)
        {
            var id = Id(key);
            records.Add(new Incident
            {
                Id = id, IncidentNumber = $"DEMO18-{key}", Title = $"DEMO18-{key} {title}",
                Description = $"DEMO18 synthetic exercise only. {description}",
                IncidentTypeId = types[type], ReporterUserId = reporterId,
                Status = IncidentStatus.Submitted, Severity = SeverityLevel.Low, Priority = PriorityLevel.Normal,
                PeopleAffected = people, VehiclesAffected = vehicles, Casualties = null,
                ReportedAtUtc = reportedAtUtc, DuplicateDetectionStatus = DuplicateDetectionStatus.NotStarted
            });
            records.Add(new IncidentLocation
            {
                Id = Id($"location/{key}"), IncidentId = id, IsPrimary = true,
                Latitude = lat, Longitude = lon, Address = $"DEMO18 synthetic location {key}",
                City = "DEMO18 Exercise City", District = "DEMO18 Exercise District", State = "DEMO18 Exercise State"
            });
            records.Add(new IncidentStatusHistory
            {
                Id = Id($"history/{key}"), IncidentId = id,
                OldStatus = IncidentStatus.Submitted, NewStatus = IncidentStatus.Submitted,
                ChangedByUserId = operatorId, ChangedAtUtc = reportedAtUtc,
                Remarks = "DEMO18 imported synthetic report. No intake job or AI processing was executed."
            });
            records.Add(new IncidentNote
            {
                Id = Id($"note/{key}"), IncidentId = id, CreatedByUserId = operatorId,
                Content = "DEMO18 fixture. Low/Normal are intake defaults, not triage findings. Null counts mean unknown. Duplicate candidates require the real detection pipeline; overlapping reports are not confirmed duplicates."
            });
        }

        var resourceDefinitions = new[]
        {
            ("BOAT", ResourceType.Boat, "DISASTER"),
            ("FIRE-TRUCK", ResourceType.FireTruck, "FIRE"),
            ("POLICE", ResourceType.PoliceVehicle, "POLICE"),
            ("RESCUE", ResourceType.RescueTeam, "DISASTER"),
            ("AMBULANCE", ResourceType.Ambulance, "MEDICAL"),
            ("EXCAVATOR", ResourceType.Excavator, "PWD")
        };
        foreach (var (key, type, department) in resourceDefinitions)
        {
            records.Add(new Resource
            {
                Id = Id($"resource/{key}"), Name = $"DEMO18-{key} synthetic unit",
                Type = type, Status = ResourceStatus.Available, DepartmentId = departments[department],
                Latitude = 18.522000m, Longitude = 73.858000m, LocationUpdatedAtUtc = reportedAtUtc,
                Capacity = 2, ShowOnOperationalMap = true
            });
        }

        foreach (var key in new[] { "FIRE-01", "COLLAPSE-01" })
        {
            records.Add(new IncidentEscalation
            {
                Id = Id($"escalation/{key}"), IncidentId = Id(key), Type = EscalationType.ManualEscalation,
                Level = EscalationLevel.Elevated, Status = EscalationStatus.Open, RequiresHumanReview = true,
                Reason = $"DEMO18 manual exercise escalation for {key}: occupancy/casualties are unknown. Review the synthetic report; this is not an AI or rule-engine conclusion.",
                EvidenceJson = "[\"DEMO18 synthetic manual exercise record; uncertainty retained\"]", Score = null
            });
        }

        var situationId = Id("situation/flood");
        records.Add(new OperationalSituation
        {
            Id = situationId, SituationNumber = "DEMO18-SITUATION-FLOOD", Title = "DEMO18 manual flood report group",
            Summary = "DEMO18 manual exercise group of three overlapping reports. Six people is the sum of reported counts and may double-count the same two occupants. Casualty total 0 is the model's aggregate default for unknown counts, not a confirmed absence of casualties. No AI summary or automatic clustering was run.",
            Level = EscalationLevel.Advisory, Status = EscalationStatus.Open,
            IncidentCount = 3, CriticalIncidentCount = 0, HighIncidentCount = 0,
            PeopleAffected = 6, Casualties = 0, RequiresHumanReview = true, SituationScore = null
        });
        foreach (var key in new[] { "FLOOD-01", "FLOOD-02", "DUPLICATE-01" })
        {
            records.Add(new OperationalSituationIncident
            {
                Id = Id($"membership/{key}"), IncidentId = Id(key), OperationalSituationId = situationId,
                IsPrimaryIncident = key == "FLOOD-01", AddedAtUtc = reportedAtUtc
            });
        }
        foreach (var key in new[] { "FIRE-01", "COLLAPSE-01", "flood-group" })
        {
            var group = key == "flood-group";
            records.Add(new Notification
            {
                Id = Id($"notification/{key}"), UserId = operatorId, TargetRole = null,
                Type = NotificationType.System, Priority = NotificationPriority.Important,
                Status = NotificationStatus.Unread, IsRead = false,
                Title = $"DEMO18-{key} exercise review", Message = "DEMO18 seeded manual review reminder. Synthetic data only; no AI, dispatch, or realtime delivery is implied.",
                IncidentId = group ? null : Id(key), OperationalSituationId = group ? situationId : null,
                IncidentEscalationId = group ? null : Id($"escalation/{key}"),
                DeduplicationKey = $"{Key}/notification/{key}", MetadataJson = "{\"owner\":\"DEMO18-v1\",\"synthetic\":true}"
            });
        }
        return records;
    }
}
