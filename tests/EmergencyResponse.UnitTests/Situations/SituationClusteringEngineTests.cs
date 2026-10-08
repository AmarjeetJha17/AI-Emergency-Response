using EmergencyResponse.Application.Situations;
using EmergencyResponse.Domain.Enums;
using Xunit;

namespace EmergencyResponse.UnitTests.Situations;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public class SituationClusteringEngineTests
{
    [Fact]
    public void BuildClusters_WhenFewerThanMinimumIncidents_ReturnsEmpty()
    {
        var candidates = new List<SituationCandidate>
        {
            CreateCandidate(19.0760, 72.8777, DateTime.UtcNow),
            CreateCandidate(19.0770, 72.8780, DateTime.UtcNow)
        };

        var clusters = SituationClusteringEngine.BuildClusters(candidates, 10.0, 6.0, 3);

        Assert.Empty(clusters);
    }

    [Fact]
    public void BuildClusters_WhenThreeNearbyIncidentsWithinTimeWindow_ReturnsSingleCluster()
    {
        var now = DateTime.UtcNow;
        var candidates = new List<SituationCandidate>
        {
            CreateCandidate(19.0760, 72.8777, now),
            CreateCandidate(19.0800, 72.8800, now.AddMinutes(30)),
            CreateCandidate(19.0820, 72.8820, now.AddHours(1))
        };

        var clusters = SituationClusteringEngine.BuildClusters(candidates, 10.0, 6.0, 3);

        Assert.Single(clusters);
        Assert.Equal(3, clusters[0].Count);
    }

    [Fact]
    public void BuildClusters_WhenChainLinked_GroupsAllIncidents()
    {
        // A -> B (5 km), B -> C (5 km), A -> C (~10 km)
        var now = DateTime.UtcNow;
        var a = CreateCandidate(19.0000, 72.8000, now);
        var b = CreateCandidate(19.0400, 72.8000, now.AddMinutes(15));
        var c = CreateCandidate(19.0800, 72.8000, now.AddMinutes(30));

        var clusters = SituationClusteringEngine.BuildClusters([a, b, c], 6.0, 6.0, 3);

        Assert.Single(clusters);
        Assert.Equal(3, clusters[0].Count);
    }

    [Fact]
    public void BuildClusters_WhenIncidentExceedsDistance_ExcludesFarIncident()
    {
        var now = DateTime.UtcNow;
        var near1 = CreateCandidate(19.0760, 72.8777, now);
        var near2 = CreateCandidate(19.0800, 72.8800, now.AddMinutes(20));
        var near3 = CreateCandidate(19.0820, 72.8820, now.AddMinutes(40));
        // 500 km away (Delhi)
        var far = CreateCandidate(28.6139, 77.2090, now);

        var clusters = SituationClusteringEngine.BuildClusters([near1, near2, near3, far], 10.0, 6.0, 3);

        Assert.Single(clusters);
        Assert.Equal(3, clusters[0].Count);
        Assert.DoesNotContain(far, clusters[0]);
    }

    [Fact]
    public void BuildClusters_WhenIncidentExceedsTimeWindow_ExcludesOldIncident()
    {
        var now = DateTime.UtcNow;
        var inc1 = CreateCandidate(19.0760, 72.8777, now);
        var inc2 = CreateCandidate(19.0800, 72.8800, now.AddMinutes(30));
        var inc3 = CreateCandidate(19.0820, 72.8820, now.AddMinutes(60));
        // 24 hours earlier
        var oldInc = CreateCandidate(19.0770, 72.8780, now.AddHours(-24));

        var clusters = SituationClusteringEngine.BuildClusters([inc1, inc2, inc3, oldInc], 10.0, 6.0, 3);

        Assert.Single(clusters);
        Assert.Equal(3, clusters[0].Count);
        Assert.DoesNotContain(oldInc, clusters[0]);
    }

    [Fact]
    public void BuildClusters_IncludesIncidentExactlyAtTimeBoundary()
    {
        var start = DateTime.UtcNow;
        var first = CreateCandidate(19.0760, 72.8777, start);
        var atBoundary = CreateCandidate(19.0760, 72.8777, start.AddHours(6));

        var clusters = SituationClusteringEngine.BuildClusters(
            [first, atBoundary], maximumDistanceKm: 1, maximumTimeWindowHours: 6,
            minimumIncidentCount: 2);

        Assert.Single(clusters);
        Assert.Equal(2, clusters[0].Count);
    }

    [Fact]
    public void BuildClusters_ExcludesIncidentJustOutsideTimeBoundary()
    {
        var start = DateTime.UtcNow;
        var first = CreateCandidate(19.0760, 72.8777, start);
        var justOutside = CreateCandidate(19.0760, 72.8777, start.AddHours(6).AddTicks(1));

        var clusters = SituationClusteringEngine.BuildClusters(
            [first, justOutside], maximumDistanceKm: 1, maximumTimeWindowHours: 6,
            minimumIncidentCount: 2);

        Assert.Empty(clusters);
    }

    private static SituationCandidate CreateCandidate(double lat, double lon, DateTime reportedAt)
    {
        return new SituationCandidate
        {
            IncidentId = Guid.NewGuid(),
            IncidentNumber = "INC-" + Guid.NewGuid().ToString("N")[..8],
            Title = "Test Emergency",
            IncidentTypeId = Guid.NewGuid(),
            IncidentTypeName = "Fire",
            Severity = SeverityLevel.Moderate,
            Priority = PriorityLevel.Normal,
            Latitude = lat,
            Longitude = lon,
            PeopleAffected = 5,
            Casualties = 0,
            CreatedAtUtc = reportedAt
        };
    }
}
