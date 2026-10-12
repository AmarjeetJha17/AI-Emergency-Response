using EmergencyResponse.Application.Predictive.Escalation;

namespace EmergencyResponse.UnitTests.Predictive;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class EscalationPredictionFeaturesTests
{
    [Fact]
    public void Features_default_to_zero_with_no_environmental_context()
    {
        var features = new EscalationPredictionFeatures();

        Assert.Equal(0f, features.SeverityScore);
        Assert.Equal(0f, features.PriorityScore);
        Assert.Equal(0f, features.PeopleAffected);
        Assert.Equal(0f, features.VehiclesAffected);
        Assert.Equal(0f, features.Casualties);
        Assert.Equal(0f, features.ImmediateDanger);
        Assert.Equal(0f, features.PeopleTrapped);
        Assert.Equal(0f, features.CriticalInfrastructureAffected);
        Assert.Equal(0f, features.InfrastructureDamage);
        Assert.Equal(0f, features.RoadBlocked);
        Assert.Equal(0f, features.EscalationCount);
        Assert.Equal(0f, features.DuplicateCount);
        Assert.Equal(0f, features.RelatedIncidentCount);
        Assert.Equal(0f, features.ResourceShortage);
        Assert.Equal(0f, features.IncidentAgeMinutes);
        Assert.Equal(0f, features.PrecipitationMm);
        Assert.Equal(0f, features.WindSpeedKph);
        Assert.Equal(0f, features.VisibilityKm);
        Assert.Equal(0f, features.SevereWeatherAlert);
        Assert.Equal(0f, features.AirQualityIndex);
        Assert.Equal(0f, features.SignificantTrafficDisruption);
        Assert.False(features.EnvironmentalContextAvailable);
    }

    [Fact]
    public void Features_preserve_all_operational_and_environmental_values()
    {
        var expected = new EscalationPredictionFeatures
        {
            SeverityScore = 1,
            PriorityScore = 2,
            PeopleAffected = 3,
            VehiclesAffected = 4,
            Casualties = 5,
            ImmediateDanger = 6,
            PeopleTrapped = 7,
            CriticalInfrastructureAffected = 8,
            InfrastructureDamage = 9,
            RoadBlocked = 10,
            EscalationCount = 11,
            DuplicateCount = 12,
            RelatedIncidentCount = 13,
            ResourceShortage = 14,
            IncidentAgeMinutes = 15,
            PrecipitationMm = 16,
            WindSpeedKph = 17,
            VisibilityKm = 18,
            SevereWeatherAlert = 19,
            AirQualityIndex = 20,
            SignificantTrafficDisruption = 21,
            EnvironmentalContextAvailable = true
        };

        var actualValues = new[]
        {
            expected.SeverityScore,
            expected.PriorityScore,
            expected.PeopleAffected,
            expected.VehiclesAffected,
            expected.Casualties,
            expected.ImmediateDanger,
            expected.PeopleTrapped,
            expected.CriticalInfrastructureAffected,
            expected.InfrastructureDamage,
            expected.RoadBlocked,
            expected.EscalationCount,
            expected.DuplicateCount,
            expected.RelatedIncidentCount,
            expected.ResourceShortage,
            expected.IncidentAgeMinutes,
            expected.PrecipitationMm,
            expected.WindSpeedKph,
            expected.VisibilityKm,
            expected.SevereWeatherAlert,
            expected.AirQualityIndex,
            expected.SignificantTrafficDisruption
        };

        Assert.Equal(Enumerable.Range(1, 21).Select(value => (float)value), actualValues);
        Assert.True(expected.EnvironmentalContextAvailable);
    }
}
