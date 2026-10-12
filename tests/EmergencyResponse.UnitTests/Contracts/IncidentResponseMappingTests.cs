using System.Text.Json;
using EmergencyResponse.Application.AI.VisualAnalysis;
using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Reliability;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Services;
using EmergencyResponse.UnitTests.Infrastructure;
using Moq;

namespace EmergencyResponse.UnitTests.Contracts;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class IncidentResponseMappingTests
{
    [Fact]
    public async Task GetById_maps_incident_and_nested_visual_and_escalation_responses()
    {
        await using var fixture = await SqliteApplicationDbFixture.CreateAsync();
        var incidentType = new IncidentType
        {
            Id = Guid.NewGuid(),
            Name = "Flood",
            Code = "FLOOD",
            IsActive = true
        };
        var incidentId = Guid.NewGuid();
        var relatedIncidentId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            IncidentNumber = "INC-2026-0091",
            Title = "Flooded underpass",
            Description = "Water is rising in the underpass.",
            IncidentTypeId = incidentType.Id,
            IncidentType = incidentType,
            Status = IncidentStatus.Escalated,
            Severity = SeverityLevel.High,
            Priority = PriorityLevel.High,
            PeopleAffected = 12,
            VehiclesAffected = 4,
            Casualties = 1,
            ReportedAtUtc = Utc(9, 20),
            SeverityScore = 82,
            PriorityScore = 91,
            SeverityReasonsJson = "[\"people affected\",\"critical location\"]",
            PriorityReasonsJson = "[\"rising water\"]",
            DuplicateDetectionStatus = DuplicateDetectionStatus.Completed,
            IsCanonical = false,
            Locations =
            [
                new IncidentLocation
                {
                    Id = Guid.NewGuid(),
                    Latitude = 28.6139m,
                    Longitude = 77.2090m,
                    Address = "Central underpass",
                    City = "New Delhi",
                    District = "Central",
                    State = "Delhi",
                    IsPrimary = true
                }
            ],
            StatusHistory =
            [
                new IncidentStatusHistory
                {
                    Id = Guid.NewGuid(),
                    OldStatus = IncidentStatus.UnderReview,
                    NewStatus = IncidentStatus.Escalated,
                    ChangedByUserId = "operator-1",
                    Remarks = "Water level increased.",
                    ChangedAtUtc = Utc(9, 45)
                }
            ],
            Media =
            [
                new IncidentMedia
                {
                    Id = mediaId,
                    FileName = "underpass.mp4",
                    ContentType = "video/mp4",
                    FileSizeBytes = 2048,
                    StoragePath = "media/underpass.mp4",
                    StorageKey = "media/underpass.mp4",
                    SizeBytes = 2048,
                    MediaType = IncidentMediaType.Video,
                    AIAnalysisRequested = true
                }
            ]
        };
        var relatedIncident = new Incident
        {
            Id = relatedIncidentId,
            IncidentNumber = "INC-2026-0088",
            Title = "Nearby water rescue",
            Description = "Water rescue reported nearby.",
            IncidentTypeId = incidentType.Id,
            Status = IncidentStatus.InProgress,
            Severity = SeverityLevel.Moderate,
            Priority = PriorityLevel.High,
            ReportedAtUtc = Utc(9, 10)
        };
        var findings = new VisualAnalysisResult
        {
            SceneDescription = "Water covers the road.",
            DetectedHazards = ["deep water", "fast current"],
            VisibleEmergencyIndicators = ["stranded vehicle"],
            EstimatedPeopleAffected = 7,
            EstimatedVehiclesAffected = 2,
            PeopleAppearTrapped = true,
            RoadAppearsBlocked = true,
            StructuralDamageVisible = true,
            FireVisible = false,
            FloodingVisible = true,
            SmokeVisible = false,
            ImmediateDangerVisible = true,
            RecommendedResponse = "Dispatch high-water rescue.",
            Confidence = 0.94,
            FrameFindings =
            [
                new VisualFrameFinding
                {
                    FrameNumber = 3,
                    TimestampSeconds = 9.5,
                    SceneDescription = "A vehicle is surrounded by water.",
                    DetectedHazards = ["fast current"],
                    EstimatedPeopleAffected = 2,
                    EstimatedVehiclesAffected = 1,
                    PeopleAppearTrapped = true,
                    RoadAppearsBlocked = true,
                    StructuralDamageVisible = false,
                    FireVisible = false,
                    FloodingVisible = true,
                    SmokeVisible = false,
                    ImmediateDangerVisible = true,
                    Confidence = 0.92
                }
            ]
        };

        fixture.Context.IncidentTypes.Add(incidentType);
        fixture.Context.Incidents.AddRange(incident, relatedIncident);
        fixture.Context.IncidentDuplicateMatches.Add(new IncidentDuplicateMatch
        {
            Id = Guid.NewGuid(),
            SourceIncidentId = incidentId,
            CandidateIncidentId = relatedIncidentId,
            DuplicateScore = 0.91,
            Reviewed = false
        });
        fixture.Context.IncidentRelations.Add(new IncidentRelation
        {
            Id = Guid.NewGuid(),
            ParentIncidentId = incidentId,
            RelatedIncidentId = relatedIncidentId,
            RelationType = IncidentRelationType.Duplicate
        });
        fixture.Context.VisualAnalyses.Add(new VisualAnalysis
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            IncidentMediaId = mediaId,
            ModelName = "vision-model",
            PromptVersion = "vision-prompt-v2",
            Status = VisualAnalysisStatus.Completed,
            FindingsJson = JsonSerializer.Serialize(findings),
            Confidence = 0.88,
            HumanReviewed = true,
            HumanDecision = "Confirmed flood conditions.",
            FramesAnalyzed = 4,
            VideoDurationSeconds = 15.5
        });
        fixture.Context.IncidentEscalations.Add(new IncidentEscalation
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            Type = EscalationType.ResourceShortage,
            Level = EscalationLevel.Critical,
            Status = EscalationStatus.Acknowledged,
            Reason = "No high-water rescue unit is nearby.",
            Score = 0.97,
            RequiresHumanReview = true,
            AcknowledgedByUserId = "operator-2",
            AcknowledgedAtUtc = Utc(9, 50),
            ResolvedByUserId = "supervisor-1",
            ResolvedAtUtc = Utc(10, 5),
            ResolutionNotes = "A rescue boat is being dispatched.",
            CreatedAtUtc = Utc(9, 40)
        });
        await fixture.Context.SaveChangesAsync();

        var service = new IncidentService(
            fixture.Context,
            Mock.Of<IIncidentNumberGenerator>(),
            Mock.Of<ICurrentUserService>(),
            Mock.Of<IBackgroundJobScheduler>());

        var response = await service.GetByIdAsync(incidentId);

        Assert.NotNull(response);
        Assert.Equal(incidentId, response.Id);
        Assert.Equal("INC-2026-0091", response.IncidentNumber);
        Assert.Equal("Flood", response.IncidentType);
        Assert.Equal("Escalated", response.Status);
        Assert.Equal(DuplicateDetectionStatus.Completed, response.DuplicateDetectionStatus);
        Assert.True(response.HasDuplicateCandidates);
        Assert.Equal(relatedIncidentId, Assert.Single(response.RelatedIncidents).Id);
        Assert.Equal(new[] { "people affected", "critical location" }, response.SeverityReasons);
        Assert.Equal(new[] { "rising water" }, response.PriorityReasons);
        Assert.Equal(28.6139m, response.Latitude);
        Assert.Equal(77.2090m, response.Longitude);
        Assert.Equal("Central underpass", response.Address);
        Assert.Equal("New Delhi", response.City);
        Assert.Equal(12, response.PeopleAffected);
        Assert.Equal(4, response.VehiclesAffected);
        Assert.Equal(1, response.Casualties);
        Assert.Equal("Escalated", Assert.Single(response.StatusHistory).NewStatus);

        var media = Assert.Single(response.VisualEvidence);
        Assert.Equal("video/mp4", media.ContentType);
        Assert.Equal(IncidentMediaType.Video, media.MediaType);
        Assert.Equal(VisualAnalysisStatus.Completed, media.VisualAnalysisStatus);
        var analysis = Assert.IsType<VisualAnalysisResponse>(media.Analysis);
        Assert.Equal("Water covers the road.", analysis.SceneDescription);
        Assert.Equal(new[] { "deep water", "fast current" }, analysis.DetectedHazards);
        Assert.Equal(new[] { "stranded vehicle" }, analysis.VisibleEmergencyIndicators);
        Assert.Equal(7, analysis.EstimatedPeopleAffected);
        Assert.Equal(2, analysis.EstimatedVehiclesAffected);
        Assert.True(analysis.PeopleAppearTrapped);
        Assert.True(analysis.RoadAppearsBlocked);
        Assert.True(analysis.StructuralDamageVisible);
        Assert.True(analysis.FloodingVisible);
        Assert.True(analysis.ImmediateDangerVisible);
        Assert.Equal("Dispatch high-water rescue.", analysis.RecommendedResponse);
        Assert.Equal(0.94, analysis.Confidence);
        Assert.Equal("Confirmed flood conditions.", analysis.HumanDecision);
        Assert.Equal("Accepted", analysis.HumanReviewOutcome);
        var frame = Assert.Single(analysis.FrameFindings);
        Assert.Equal(3, frame.FrameNumber);
        Assert.Equal(9.5, frame.TimestampSeconds);
        Assert.Equal("A vehicle is surrounded by water.", frame.SceneDescription);
        Assert.Equal(new[] { "fast current" }, frame.DetectedHazards);
        Assert.True(frame.PeopleAppearTrapped);
        Assert.True(frame.RoadAppearsBlocked);
        Assert.True(frame.FloodingVisible);
        Assert.True(frame.ImmediateDangerVisible);

        var escalation = Assert.Single(response.ActiveEscalations);
        Assert.Equal(incidentId, escalation.IncidentId);
        Assert.Equal("INC-2026-0091", escalation.IncidentNumber);
        Assert.Equal("ResourceShortage", escalation.TypeName);
        Assert.Equal("Critical", escalation.LevelName);
        Assert.Equal("Acknowledged", escalation.StatusName);
        Assert.Equal("No high-water rescue unit is nearby.", escalation.Reason);
        Assert.Equal("operator-2", escalation.AcknowledgedByUserName);
        Assert.Equal("supervisor-1", escalation.ResolvedByUserName);
        Assert.Equal("A rescue boat is being dispatched.", escalation.ResolutionNotes);
    }

    private static DateTime Utc(int hour, int minute)
        => new(2026, 10, 12, hour, minute, 0, DateTimeKind.Utc);
}
