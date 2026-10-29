using EmergencyResponse.Infrastructure.Persistence.Seed;

namespace EmergencyResponse.UnitTests.Infrastructure;

public sealed class HostedPortfolioStagesTests
{
    private const string Secret = "Password=secret-marker;AccessToken=private-token;Server=private-server;SELECT private_statement";

    [Fact]
    public void UnknownFailuresPrintOnlyStageAndType()
    {
        foreach (var stage in Enum.GetValues<HostedPortfolioStage>())
        {
            var events = new List<string>();
            var stages = new HostedPortfolioStages(events.Add);
            stages.Enter(stage);
            var exception = new InvalidOperationException(Secret, new Exception(Secret));
            exception.Data["connection"] = Secret;
            Assert.Equal($"HP999; Stage={HostedPortfolioStages.Code(stage)}; ExceptionType=InvalidOperationException", stages.Failure(exception));
            Assert.Single(events);
            Assert.DoesNotContain("completed", events[0]);
            Assert.DoesNotContain("secret-marker", stages.Failure(exception));
            Assert.DoesNotContain("private", stages.Failure(exception));
        }
    }

    [Fact]
    public async Task MissingReferencesKeepFailedStageIncomplete()
    {
        await using var fixture = await SqliteApplicationDbFixture.CreateAsync();
        var events = new List<string>();
        var stages = new HostedPortfolioStages(events.Add);
        var failure = await Assert.ThrowsAsync<HostedPortfolioException>(() =>
            HostedPortfolioSeeder.PreviewAsync(fixture.Context, stages: stages));
        Assert.Equal(new[] { "ST002 started", "ST002 completed", "ST005 started" }, events);
        Assert.StartsWith("HP005;", stages.Failure(failure));
        Assert.EndsWith("Stage=ST005", stages.Failure(failure));
    }

    [Fact]
    public async Task CompletedPreviewReportsOnlyOperationsThatFinished()
    {
        await using var fixture = await SqliteApplicationDbFixture.CreateAsync();
        await DatabaseSeeder.SeedAsync(fixture.Context);
        var events = new List<string>();
        await HostedPortfolioSeeder.PreviewAsync(fixture.Context, stages: new HostedPortfolioStages(events.Add));
        Assert.Equal(new[] { "ST002 started", "ST002 completed", "ST005 started", "ST005 completed",
            "ST007 started", "ST007 completed", "ST006 started", "ST006 completed", "ST009 started", "ST009 completed" }, events);
        Assert.DoesNotContain(events, x => x.StartsWith("ST003")); // SQLite helper never claims SQL was opened.
    }

    [Theory]
    [InlineData("master", "EmergencyResponse")]
    [InlineData("EmergencyResponse_PortfolioDemo", "EmergencyResponse")]
    [InlineData(null, "EmergencyResponse")]
    public void ActualDatabaseMustMatchValidatedConfiguredTarget(string? actual, string expected)
    {
        var error = Assert.Throws<HostedPortfolioException>(() => HostedPortfolioSeeder.VerifyDatabaseName(actual, expected));
        var stages = new HostedPortfolioStages();
        stages.Enter(HostedPortfolioStage.VerifyTarget);
        Assert.StartsWith("HP004;", stages.Failure(error));
        Assert.EndsWith("Stage=ST004", stages.Failure(error));
    }
}
