using EmergencyResponse.Application.Common.Geo;
using EmergencyResponse.Application.Situations.DTOs;

namespace EmergencyResponse.Application.Situations;

/// <summary>
/// Calculates centroid and awareness radius envelope for a collection of member incidents.
/// Note: This circle represents an operational awareness visualization derived from member incidents.
/// It is NOT an official geographic boundary, evacuation zone, service area, or dispatch perimeter.
/// </summary>
public static class SituationEnvelopeCalculator
{
    public static (double CenterLatitude, double CenterLongitude, double RadiusMeters) CalculateEnvelope(
        IReadOnlyList<SituationMapIncident> incidents)
    {
        if (incidents == null || incidents.Count == 0)
        {
            throw new InvalidOperationException("A situation cannot be mapped without incidents.");
        }

        var centerLat = incidents.Average(x => x.Latitude);
        var centerLon = incidents.Average(x => x.Longitude);

        var maxDistanceKm = incidents
            .Select(x => GeoDistance.CalculateKilometers(centerLat, centerLon, x.Latitude, x.Longitude))
            .DefaultIfEmpty(0.0)
            .Max();

        // 1.20 expansion visualization buffer; minimum awareness radius is 0.5 km (500 meters)
        var radiusKm = Math.Max(maxDistanceKm * 1.20, 0.5);

        return (centerLat, centerLon, radiusKm * 1000.0);
    }
}
