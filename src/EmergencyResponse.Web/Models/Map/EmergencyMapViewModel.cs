using EmergencyResponse.Application.Map;

namespace EmergencyResponse.Web.Models.Map;

public sealed class EmergencyMapViewModel
{
    public IReadOnlyList<EmergencyMapIncident> Incidents { get; set; } = [];
    public IReadOnlyList<MapResource> Resources { get; set; } = [];
    public IReadOnlyList<MapOperationalZone> OperationalZones { get; set; } = [];
    public IReadOnlyList<string> IncidentTypes { get; init; } = [];
    public IReadOnlyList<string> Departments { get; init; } = [];
    public Guid? SelectedIncidentId { get; init; }
    public string TileUrl { get; set; } = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
    public string Attribution { get; set; } = "&copy; <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors";
    public double DefaultLatitude { get; set; } = 20.5937;
    public double DefaultLongitude { get; set; } = 78.9629;
    public int DefaultZoom { get; set; } = 5;
}
