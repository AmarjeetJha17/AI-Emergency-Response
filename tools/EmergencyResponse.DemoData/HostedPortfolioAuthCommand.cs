using Azure.Identity;
using EmergencyResponse.Infrastructure.Persistence.Seed;

internal static class HostedPortfolioAuthCommand
{
    internal static async Task<int> RunAsync(string[] args)
    {
        try
        {
            if (args.Length != 1) throw new HostedPortfolioException(HostedPortfolioFailure.Configuration);
            var connection = Environment.GetEnvironmentVariable("PORTFOLIO_SEED_CONNECTION");
            if (string.IsNullOrWhiteSpace(connection)) throw new HostedPortfolioException(HostedPortfolioFailure.Configuration);
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            return await HostedPortfolioAuthProbe.RunAsync(connection, new DefaultAzureCredential(),
                Console.WriteLine, timeout.Token);
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"TOKEN_FAIL; ExceptionType={HostedPortfolioStages.SafeTypeName(error.GetType())}");
            return 1;
        }
    }
}
