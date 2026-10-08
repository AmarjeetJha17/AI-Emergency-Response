using EmergencyResponse.Application.Predictive.Forecasting;
using EmergencyResponse.Domain.Predictive;

namespace EmergencyResponse.UnitTests.Predictive.Forecasting;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class TimeSeriesBucketBuilderTests
{
    [Fact]
    public void FillMissingBuckets_InsertsZeroForMissingHours()
    {
        var start = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var values = new Dictionary<DateTime, float>
        {
            [start] = 5,
            [start.AddHours(2)] = 3
        };

        var result = TimeSeriesBucketBuilder.FillMissingBuckets(
            values,
            start,
            start.AddHours(2),
            ForecastGranularity.Hour);

        Assert.Equal(3, result.Count);
        Assert.Equal(5, result[0].Value);
        Assert.Equal(0, result[1].Value);
        Assert.Equal(3, result[2].Value);
    }
}
