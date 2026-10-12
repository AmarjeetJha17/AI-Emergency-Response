namespace EmergencyResponse.IntegrationTests.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string Name = "SQL Server integration database";
}

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    public EmergencyResponseWebApplicationFactory Factory { get; } = new();

    public async Task InitializeAsync()
    {
        await SqlServerReadiness.WaitForSqlServerAsync();
        using var client = Factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        Factory.Dispose();
        return Task.CompletedTask;
    }
}
