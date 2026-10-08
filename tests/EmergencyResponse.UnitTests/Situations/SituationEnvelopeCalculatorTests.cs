using EmergencyResponse.Application.Situations;
using EmergencyResponse.Application.Situations.DTOs;
using EmergencyResponse.Domain.Enums;
using Xunit;

namespace EmergencyResponse.UnitTests.Situations;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public class SituationEnvelopeCalculatorTests
{
    [Fact]
    public void CalculateEnvelope_WithEmptyIncidents_ThrowsInvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => SituationEnvelopeCalculator.CalculateEnvelope([]));
        Assert.Contains("cannot be mapped without incidents", ex.Message);
    }

    [Fact]
    public void CalculateEnvelope_WithSingleIncident_ReturnsIncidentCoordsAndMinRadius500m()
    {
        var incidents = new List<SituationMapIncident>
        {
            new()
            {
                IncidentId = Guid.NewGuid(),
                Latitude = 28.6139,
                Longitude = 77.2090,
                Severity = SeverityLevel.High
            }
        };

        var envelope = SituationEnvelopeCalculator.CalculateEnvelope(incidents);

        Assert.Equal(28.6139, envelope.CenterLatitude, 4);
        Assert.Equal(77.2090, envelope.CenterLongitude, 4);
        Assert.Equal(500.0, envelope.RadiusMeters); // 0.5 km minimum buffer = 500m
    }

    [Fact]
    public void CalculateEnvelope_WithMultipleIncidents_CalculatesCentroidAnd120PercentBuffer()
    {
        var incidents = new List<SituationMapIncident>
        {
            new()
            {
                IncidentId = Guid.NewGuid(),
                Latitude = 28.6000,
                Longitude = 77.2000,
                Severity = SeverityLevel.Critical
            },
            new()
            {
                IncidentId = Guid.NewGuid(),
                Latitude = 28.6200,
                Longitude = 77.2000,
                Severity = SeverityLevel.High
            },
            new()
            {
                IncidentId = Guid.NewGuid(),
                Latitude = 28.6100,
                Longitude = 77.2200,
                Severity = SeverityLevel.Moderate
            }
        };

        var envelope = SituationEnvelopeCalculator.CalculateEnvelope(incidents);

        var expectedLat = (28.6000 + 28.6200 + 28.6100) / 3.0;
        var expectedLon = (77.2000 + 77.2000 + 77.2200) / 3.0;

        Assert.Equal(expectedLat, envelope.CenterLatitude, 4);
        Assert.Equal(expectedLon, envelope.CenterLongitude, 4);
        Assert.True(envelope.RadiusMeters > 500.0);
    }
}
