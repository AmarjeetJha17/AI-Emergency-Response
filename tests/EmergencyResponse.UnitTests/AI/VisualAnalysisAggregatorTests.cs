using EmergencyResponse.Application.AI.VisualAnalysis;

namespace EmergencyResponse.UnitTests.AI;

/// <summary>
/// Unit tests for VisualAnalysisAggregator.Aggregate().
/// Covers the aggregation rules required by task 4.23.
/// </summary>
public sealed class VisualAnalysisAggregatorTests
{
    // ----------------------------------------------------------------
    // Helper
    // ----------------------------------------------------------------

    private static VisualFrameFinding Frame(
        int frameNumber = 0,
        double timestampSeconds = 0.0,
        bool fireVisible = false,
        bool floodingVisible = false,
        bool roadAppearsBlocked = false,
        bool immediateDangerVisible = false,
        bool smokeVisible = false,
        bool structuralDamageVisible = false,
        bool peopleAppearTrapped = false,
        int? estimatedPeopleAffected = null,
        int? estimatedVehiclesAffected = null,
        double confidence = 0.8,
        IEnumerable<string>? detectedHazards = null)
    {
        return new VisualFrameFinding
        {
            FrameNumber = frameNumber,
            TimestampSeconds = timestampSeconds,
            SceneDescription = $"Frame {frameNumber}",
            FireVisible = fireVisible,
            FloodingVisible = floodingVisible,
            RoadAppearsBlocked = roadAppearsBlocked,
            ImmediateDangerVisible = immediateDangerVisible,
            SmokeVisible = smokeVisible,
            StructuralDamageVisible = structuralDamageVisible,
            PeopleAppearTrapped = peopleAppearTrapped,
            EstimatedPeopleAffected = estimatedPeopleAffected,
            EstimatedVehiclesAffected = estimatedVehiclesAffected,
            Confidence = confidence,
            DetectedHazards = (detectedHazards ?? []).ToList()
        };
    }

    // ----------------------------------------------------------------
    // Fire — boolean OR
    // ----------------------------------------------------------------

    [Fact]
    public void AnyFrame_HasFire_Result_FireVisible_True()
    {
        var frames = new[]
        {
            Frame(0, fireVisible: false),
            Frame(1, fireVisible: true),
            Frame(2, fireVisible: false)
        };

        var result = VisualAnalysisAggregator.Aggregate(
            frames, null, []);

        Assert.True(result.FireVisible);
    }

    [Fact]
    public void NoFrame_HasFire_Result_FireVisible_False()
    {
        var frames = new[]
        {
            Frame(0, fireVisible: false),
            Frame(1, fireVisible: false)
        };

        var result = VisualAnalysisAggregator.Aggregate(
            frames, null, []);

        Assert.False(result.FireVisible);
    }

    // ----------------------------------------------------------------
    // Flooding — boolean OR
    // ----------------------------------------------------------------

    [Fact]
    public void AnyFrame_HasFlooding_Result_FloodingVisible_True()
    {
        var frames = new[]
        {
            Frame(0, floodingVisible: false),
            Frame(1, floodingVisible: true),
            Frame(2, floodingVisible: false)
        };

        var result = VisualAnalysisAggregator.Aggregate(
            frames, null, []);

        Assert.True(result.FloodingVisible);
    }

    [Fact]
    public void NoFrame_HasFlooding_Result_FloodingVisible_False()
    {
        var frames = new[] { Frame(0), Frame(1) };

        var result = VisualAnalysisAggregator.Aggregate(
            frames, null, []);

        Assert.False(result.FloodingVisible);
    }

    // ----------------------------------------------------------------
    // RoadAppearsBlocked — boolean OR
    // ----------------------------------------------------------------

    [Fact]
    public void AnyFrame_RoadBlocked_Result_RoadAppearsBlocked_True()
    {
        var frames = new[]
        {
            Frame(0, roadAppearsBlocked: false),
            Frame(1, roadAppearsBlocked: true)
        };

        var result = VisualAnalysisAggregator.Aggregate(
            frames, null, []);

        Assert.True(result.RoadAppearsBlocked);
    }

    // ----------------------------------------------------------------
    // ImmediateDangerVisible — boolean OR
    // ----------------------------------------------------------------

    [Fact]
    public void AnyFrame_ImmediateDanger_Result_ImmediateDangerVisible_True()
    {
        var frames = new[]
        {
            Frame(0, immediateDangerVisible: false),
            Frame(1, immediateDangerVisible: true)
        };

        var result = VisualAnalysisAggregator.Aggregate(
            frames, null, []);

        Assert.True(result.ImmediateDangerVisible);
    }

    // ----------------------------------------------------------------
    // Vehicle count — max, not sum
    // ----------------------------------------------------------------

    [Fact]
    public void VehicleCount_IsMax_NotSum()
    {
        // Frame 0 → 3, Frame 1 → 3, Frame 2 → 4.
        // Sum would be 10; conservative max is 4.
        var frames = new[]
        {
            Frame(0, estimatedVehiclesAffected: 3),
            Frame(1, estimatedVehiclesAffected: 3),
            Frame(2, estimatedVehiclesAffected: 4)
        };

        var result = VisualAnalysisAggregator.Aggregate(
            frames, null, []);

        Assert.Equal(4, result.EstimatedVehiclesAffected);
        Assert.NotEqual(10, result.EstimatedVehiclesAffected);
    }

    [Fact]
    public void PeopleCount_IsMax_NotSum()
    {
        var frames = new[]
        {
            Frame(0, estimatedPeopleAffected: 5),
            Frame(1, estimatedPeopleAffected: 8),
            Frame(2, estimatedPeopleAffected: 6)
        };

        var result = VisualAnalysisAggregator.Aggregate(
            frames, null, []);

        Assert.Equal(8, result.EstimatedPeopleAffected);
        Assert.NotEqual(19, result.EstimatedPeopleAffected);
    }

    [Fact]
    public void VehicleCount_AllNull_ResultIsNull()
    {
        var frames = new[]
        {
            Frame(0, estimatedVehiclesAffected: null),
            Frame(1, estimatedVehiclesAffected: null)
        };

        var result = VisualAnalysisAggregator.Aggregate(
            frames, null, []);

        Assert.Null(result.EstimatedVehiclesAffected);
    }

    [Fact]
    public void VehicleCount_SomeNull_MaxOfNonNullValues()
    {
        var frames = new[]
        {
            Frame(0, estimatedVehiclesAffected: null),
            Frame(1, estimatedVehiclesAffected: 7),
            Frame(2, estimatedVehiclesAffected: 2)
        };

        var result = VisualAnalysisAggregator.Aggregate(
            frames, null, []);

        Assert.Equal(7, result.EstimatedVehiclesAffected);
    }

    // ----------------------------------------------------------------
    // Hazard union
    // ----------------------------------------------------------------

    [Fact]
    public void Hazards_AreUnioned_AcrossAllFrames()
    {
        var frames = new[]
        {
            Frame(0, detectedHazards: ["Floodwater", "Debris"]),
            Frame(1, detectedHazards: ["Floodwater", "Gas leak"]),
            Frame(2, detectedHazards: ["Fire"])
        };

        var result = VisualAnalysisAggregator.Aggregate(
            frames, null, []);

        Assert.Equal(4, result.DetectedHazards.Count);
        Assert.Contains("Floodwater", result.DetectedHazards);
        Assert.Contains("Debris", result.DetectedHazards);
        Assert.Contains("Gas leak", result.DetectedHazards);
        Assert.Contains("Fire", result.DetectedHazards);
    }

    [Fact]
    public void Hazards_Deduplication_IsCaseInsensitive()
    {
        var frames = new[]
        {
            Frame(0, detectedHazards: ["floodwater"]),
            Frame(1, detectedHazards: ["FLOODWATER"]),
            Frame(2, detectedHazards: ["Floodwater"])
        };

        var result = VisualAnalysisAggregator.Aggregate(
            frames, null, []);

        Assert.Single(result.DetectedHazards);
    }

    [Fact]
    public void Hazards_EmptyAcrossAllFrames_ResultIsEmpty()
    {
        var frames = new[] { Frame(0), Frame(1) };

        var result = VisualAnalysisAggregator.Aggregate(
            frames, null, []);

        Assert.Empty(result.DetectedHazards);
    }

    // ----------------------------------------------------------------
    // Confidence — average
    // ----------------------------------------------------------------

    [Fact]
    public void Confidence_IsAverage_AcrossFrames()
    {
        var frames = new[]
        {
            Frame(0, confidence: 0.6),
            Frame(1, confidence: 0.8),
            Frame(2, confidence: 1.0)
        };

        var result = VisualAnalysisAggregator.Aggregate(
            frames, null, []);

        // (0.6 + 0.8 + 1.0) / 3 = 0.8
        Assert.Equal(0.8, result.Confidence, precision: 10);
    }

    [Fact]
    public void Confidence_SingleFrame_EqualsFrameConfidence()
    {
        var frames = new[] { Frame(0, confidence: 0.73) };

        var result = VisualAnalysisAggregator.Aggregate(
            frames, null, []);

        Assert.Equal(0.73, result.Confidence, precision: 10);
    }

    [Fact]
    public void Confidence_TwoFrames_IsExactAverage()
    {
        var frames = new[]
        {
            Frame(0, confidence: 0.5),
            Frame(1, confidence: 0.9)
        };

        var result = VisualAnalysisAggregator.Aggregate(
            frames, null, []);

        Assert.Equal(0.7, result.Confidence, precision: 10);
    }

    // ----------------------------------------------------------------
    // Passthrough / structural checks
    // ----------------------------------------------------------------

    [Fact]
    public void RecommendedResponse_IsPassedThrough()
    {
        var frames = new[] { Frame(0) };

        var result = VisualAnalysisAggregator.Aggregate(
            frames,
            recommendedResponse: "Deploy flood response team.",
            visibleEmergencyIndicators: []);

        Assert.Equal("Deploy flood response team.", result.RecommendedResponse);
    }

    [Fact]
    public void FrameFindings_ArePreservedInOutput()
    {
        var frames = new[]
        {
            Frame(0, timestampSeconds: 0.0),
            Frame(1, timestampSeconds: 8.4),
            Frame(2, timestampSeconds: 16.8)
        };

        var result = VisualAnalysisAggregator.Aggregate(
            frames, null, []);

        Assert.Equal(3, result.FrameFindings.Count);
        Assert.Equal(0.0,  result.FrameFindings[0].TimestampSeconds);
        Assert.Equal(8.4,  result.FrameFindings[1].TimestampSeconds);
        Assert.Equal(16.8, result.FrameFindings[2].TimestampSeconds);
    }

    [Fact]
    public void ZeroFrames_Throws_InvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => VisualAnalysisAggregator.Aggregate(
                [],
                recommendedResponse: null,
                visibleEmergencyIndicators: []));
    }
}
