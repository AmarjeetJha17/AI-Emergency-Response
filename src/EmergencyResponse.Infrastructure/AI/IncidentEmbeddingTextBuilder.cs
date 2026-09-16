using EmergencyResponse.Domain.Entities;

namespace EmergencyResponse.Infrastructure.AI;

public static class IncidentEmbeddingTextBuilder
{
    public static string Build(Incident incident)
    {
        var location =
            incident.Locations
                .FirstOrDefault(x => x.IsPrimary)
            ?? incident.Locations.FirstOrDefault();

        var parts = new[]
        {
            $"Incident Type: {incident.IncidentType?.Name}",
            $"Title: {incident.Title}",
            $"Description: {incident.Description}",
            $"Address: {location?.Address}",
            $"City: {location?.City}",
            $"District: {location?.District}",
            $"State: {location?.State}"
        };

        return string.Join(
            Environment.NewLine,
            parts.Where(
                x =>
                    !string.IsNullOrWhiteSpace(x) &&
                    !x.EndsWith(": ")));
    }
}