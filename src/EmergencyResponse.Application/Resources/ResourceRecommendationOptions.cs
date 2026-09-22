namespace EmergencyResponse.Application.Resources;

public sealed class ResourceRecommendationOptions
{
    public const string SectionName = "ResourceRecommendation";

    public double MaximumDistanceKm { get; set; } = 50.0;
}
