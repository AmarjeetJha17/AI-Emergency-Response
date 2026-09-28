using EmergencyResponse.Application.Common.Geo;

namespace EmergencyResponse.Application.Situations;

public static class SituationClusteringEngine
{
    public static IReadOnlyList<IReadOnlyList<SituationCandidate>> BuildClusters(
        IReadOnlyList<SituationCandidate> incidents,
        double maximumDistanceKm,
        double maximumTimeWindowHours,
        int minimumIncidentCount)
    {
        if (incidents == null || incidents.Count < minimumIncidentCount)
        {
            return [];
        }

        var unassigned = incidents
            .OrderBy(x => x.CreatedAtUtc)
            .ToList();

        var clusters = new List<IReadOnlyList<SituationCandidate>>();

        while (unassigned.Count > 0)
        {
            var seed = unassigned[0];
            unassigned.RemoveAt(0);

            var cluster = new List<SituationCandidate> { seed };
            var changed = true;

            while (changed)
            {
                changed = false;

                foreach (var candidate in unassigned.ToList())
                {
                    if (!BelongsToCluster(candidate, cluster, maximumDistanceKm, maximumTimeWindowHours))
                    {
                        continue;
                    }

                    cluster.Add(candidate);
                    unassigned.Remove(candidate);
                    changed = true;
                }
            }

            if (cluster.Count >= minimumIncidentCount)
            {
                clusters.Add(cluster);
            }
        }

        return clusters;
    }

    private static bool BelongsToCluster(
        SituationCandidate candidate,
        IReadOnlyList<SituationCandidate> cluster,
        double maximumDistanceKm,
        double maximumTimeWindowHours)
    {
        foreach (var existing in cluster)
        {
            var distance = GeoDistance.CalculateKilometers(
                existing.Latitude,
                existing.Longitude,
                candidate.Latitude,
                candidate.Longitude);

            if (distance > maximumDistanceKm)
            {
                continue;
            }

            var timeDifference = Math.Abs((existing.CreatedAtUtc - candidate.CreatedAtUtc).TotalHours);

            if (timeDifference > maximumTimeWindowHours)
            {
                continue;
            }

            return true;
        }

        return false;
    }
}
