using System.Text.Json;
using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Application.Escalation.DTOs;
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.UnitTests.Contracts;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class ResponseDtoContractTests
{
    [Fact]
    public void VisualAnalysisResponse_serialization_round_trip_preserves_all_fields()
    {
        var response = new VisualAnalysisResponse
        {
            Id = Guid.NewGuid(),
            IncidentMediaId = Guid.NewGuid(),
            Status = VisualAnalysisStatus.Completed,
            SceneDescription = "Smoke is visible near the east entrance.",
            DetectedHazards = ["smoke", "blocked exit"],
            VisibleEmergencyIndicators = ["people waving"],
            EstimatedPeopleAffected = 7,
            EstimatedVehiclesAffected = 2,
            PeopleAppearTrapped = true,
            RoadAppearsBlocked = true,
            StructuralDamageVisible = true,
            FireVisible = false,
            FloodingVisible = false,
            SmokeVisible = true,
            ImmediateDangerVisible = true,
            RecommendedResponse = "Dispatch fire and rescue units.",
            Confidence = 0.93,
            HumanReviewed = true,
            HumanDecision = "Confirmed smoke and blocked exit.",
            FramesAnalyzed = 3,
            VideoDurationSeconds = 18.5,
            FrameFindings =
            [
                new VisualFrameFindingResponse
                {
                    FrameNumber = 2,
                    TimestampSeconds = 6.25,
                    SceneDescription = "Smoke at the loading bay.",
                    DetectedHazards = ["smoke"],
                    EstimatedPeopleAffected = 7,
                    EstimatedVehiclesAffected = 2,
                    PeopleAppearTrapped = true,
                    RoadAppearsBlocked = true,
                    StructuralDamageVisible = false,
                    FireVisible = false,
                    FloodingVisible = false,
                    SmokeVisible = true,
                    ImmediateDangerVisible = true,
                    Confidence = 0.91
                }
            ]
        };

        Assert.Equal("Accepted", response.HumanReviewOutcome);
        AssertJsonRoundTrip(response);
    }

    [Theory]
    [InlineData(false, VisualAnalysisStatus.Completed, null)]
    [InlineData(true, VisualAnalysisStatus.Completed, "Accepted")]
    [InlineData(true, VisualAnalysisStatus.Rejected, "Rejected")]
    public void VisualAnalysisResponse_reports_review_outcome_from_review_state(
        bool humanReviewed,
        VisualAnalysisStatus status,
        string? expectedOutcome)
    {
        var response = new VisualAnalysisResponse
        {
            HumanReviewed = humanReviewed,
            Status = status
        };

        Assert.Equal(expectedOutcome, response.HumanReviewOutcome);
    }

    [Fact]
    public void IncidentEscalationResponse_serialization_round_trip_preserves_all_fields()
    {
        var response = new IncidentEscalationResponse
        {
            Id = Guid.NewGuid(),
            IncidentId = Guid.NewGuid(),
            IncidentNumber = "INC-2026-0091",
            IncidentTitle = "Flooded underpass",
            Type = EscalationType.ResourceShortage,
            TypeName = "Resource Shortage",
            Level = EscalationLevel.Critical,
            LevelName = "Critical",
            Status = EscalationStatus.Acknowledged,
            StatusName = "Acknowledged",
            Reason = "No available high-water rescue vehicle.",
            Evidence = ["resource-shortage", "water-depth:1.4m"],
            Score = 0.97,
            RequiresHumanReview = true,
            AcknowledgedByUserId = "operator-1",
            AcknowledgedByUserName = "Alex Operator",
            AcknowledgedAtUtc = Utc(10, 15),
            ResolvedByUserId = "supervisor-2",
            ResolvedByUserName = "Sam Supervisor",
            ResolvedAtUtc = Utc(10, 45),
            ResolutionNotes = "Rescue vehicle dispatched.",
            CreatedAtUtc = Utc(9, 50)
        };

        AssertJsonRoundTrip(response);
    }

    [Fact]
    public void IncidentResponse_serialization_round_trip_preserves_all_fields()
    {
        var escalation = new IncidentEscalationResponse
        {
            Id = Guid.NewGuid(),
            IncidentId = Guid.NewGuid(),
            IncidentNumber = "INC-2026-0091",
            IncidentTitle = "Flooded underpass",
            Type = EscalationType.ResourceShortage,
            TypeName = "Resource Shortage",
            Level = EscalationLevel.High,
            LevelName = "High",
            Status = EscalationStatus.Open,
            StatusName = "Open",
            Reason = "Rescue resources are limited.",
            Evidence = ["shortage"],
            RequiresHumanReview = true,
            CreatedAtUtc = Utc(9, 50)
        };
        var response = new IncidentResponse
        {
            Id = Guid.NewGuid(),
            IncidentNumber = "INC-2026-0091",
            Title = "Flooded underpass",
            Description = "Water is rising in the underpass.",
            IncidentType = "Flood",
            Status = "Escalated",
            DuplicateDetectionStatus = DuplicateDetectionStatus.Completed,
            HasDuplicateCandidates = true,
            CanonicalIncidentId = Guid.NewGuid(),
            CanonicalIncidentNumber = "INC-2026-0088",
            RelatedIncidents =
            [
                new RelatedIncidentResponse
                {
                    Id = Guid.NewGuid(),
                    IncidentNumber = "INC-2026-0088",
                    RelationType = "Duplicate"
                }
            ],
            Severity = "High",
            SeverityScore = 82,
            SeverityReasons = ["people affected", "critical location"],
            Priority = "Urgent",
            PriorityScore = 91,
            PriorityReasons = ["rising water"],
            Latitude = 28.6139m,
            Longitude = 77.2090m,
            Address = "Central underpass",
            City = "New Delhi",
            District = "Central",
            State = "Delhi",
            PeopleAffected = 12,
            VehiclesAffected = 4,
            Casualties = 1,
            ReportedAtUtc = Utc(9, 20),
            StatusHistory =
            [
                new IncidentStatusHistoryResponse
                {
                    OldStatus = "Submitted",
                    NewStatus = "Escalated",
                    ChangedByUserId = "operator-1",
                    Remarks = "Water level increased.",
                    ChangedAtUtc = Utc(9, 45)
                }
            ],
            AssignedResources =
            [
                new IncidentAssignedResourceResponse
                {
                    ResourceId = Guid.NewGuid(),
                    ResourceName = "Rescue Boat 2",
                    ResourceType = "Boat",
                    Status = "EnRoute",
                    AssignedAtUtc = Utc(9, 30),
                    DispatchedAtUtc = Utc(9, 35),
                    EnRouteAtUtc = Utc(9, 36),
                    OnSceneAtUtc = null,
                    ReturningAtUtc = null
                }
            ],
            ActiveEscalations = [escalation]
        };

        AssertJsonRoundTrip(response);
    }

    private static DateTime Utc(int hour, int minute)
        => new(2026, 10, 12, hour, minute, 0, DateTimeKind.Utc);

    private static void AssertJsonRoundTrip<T>(T expected)
    {
        var expectedJson = JsonSerializer.Serialize(expected);
        var actual = JsonSerializer.Deserialize<T>(expectedJson);

        Assert.NotNull(actual);
        Assert.Equal(expectedJson, JsonSerializer.Serialize(actual));
    }
}
