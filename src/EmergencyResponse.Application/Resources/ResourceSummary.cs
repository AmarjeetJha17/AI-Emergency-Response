namespace EmergencyResponse.Application.Resources;

public sealed record ResourceSummary
{
    public int Available { get; init; }
    public int Assigned { get; init; }
    public int Dispatched { get; init; }
    public int EnRoute { get; init; }
    public int OnScene { get; init; }
    public int Returning { get; init; }
    public int Unavailable { get; init; }
    public int Maintenance { get; init; }
}
