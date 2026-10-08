using EmergencyResponse.Application.Predictive.Training;

namespace EmergencyResponse.UnitTests.Predictive;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class TemporalSplitTests
{
    [Fact]
    public void Split_PreservesChronologicalBoundaries()
    {
        var start = DateTime.UtcNow.AddDays(-10);
        var rows = Enumerable.Range(0, 20)
            .Select(i => new Row(start.AddHours(i)))
            .ToList();

        var split = TemporalDatasetSplitter.Split(rows, x => x.Timestamp);

        Assert.Equal(14, split.Train.Count);
        Assert.Equal(3, split.Validation.Count);
        Assert.Equal(3, split.Test.Count);
        Assert.True(split.Train[^1].Timestamp < split.Validation[0].Timestamp);
        Assert.True(split.Validation[^1].Timestamp < split.Test[0].Timestamp);
    }

    private sealed record Row(DateTime Timestamp);
}
