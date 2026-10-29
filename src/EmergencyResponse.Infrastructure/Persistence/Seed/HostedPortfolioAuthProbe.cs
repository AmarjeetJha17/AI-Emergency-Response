using Azure.Core;
using Microsoft.Data.SqlClient;

namespace EmergencyResponse.Infrastructure.Persistence.Seed;

/// <summary>Only OpenAsync/DisposeAsync; no EF host, application queries or writes.</summary>
public static class HostedPortfolioAuthProbe
{
    public const string SqlScope = "https://database.windows.net/.default";

    public static string ExplicitTokenConnection(string original)
    {
        HostedPortfolioSeeder.ValidateConnection(original);
        var builder = new SqlConnectionStringBuilder(original);
        if (builder.Authentication != SqlAuthenticationMethod.ActiveDirectoryDefault ||
            builder.Encrypt.ToString() == "Optional" || builder.TrustServerCertificate)
            throw new HostedPortfolioException(HostedPortfolioFailure.Configuration);
        builder.Remove("Authentication");
        builder.Remove("User ID");
        builder.Remove("Password");
        builder.Remove("Integrated Security");
        // Do not retain the explicitly supplied token in a connection pool.
        builder.Pooling = false;
        return builder.ConnectionString;
    }

    public static async Task<int> RunAsync(string original, TokenCredential credential, Action<string> report,
        CancellationToken ct = default,
        Func<string, string?, CancellationToken, Task>? open = null)
    {
        AccessToken token = default;
        string? tokenValue = null;
        string explicitConnection;
        try
        {
            explicitConnection = ExplicitTokenConnection(original);
            token = await credential.GetTokenAsync(new TokenRequestContext([SqlScope]), ct);
            if (string.IsNullOrEmpty(token.Token) || token.ExpiresOn <= DateTimeOffset.UtcNow)
                throw new HostedPortfolioException(HostedPortfolioFailure.SqlAuthorization);
            tokenValue = token.Token;
            report("TOKEN_PASS");
        }
        catch (Exception error)
        {
            token = default;
            tokenValue = null;
            report($"TOKEN_FAIL; ExceptionType={HostedPortfolioStages.SafeTypeName(error.GetType())}");
            return 1;
        }

        open ??= OpenAsync;
        try
        {
            await open(explicitConnection, tokenValue, ct);
            report("SQL_OPEN_PASS");
        }
        catch (Exception error)
        {
            if (error is SqlException)
            {
                var stage = new HostedPortfolioStages();
                stage.Enter(HostedPortfolioStage.OpenConnection);
                report($"SQL_OPEN_FAIL; {stage.Failure(error)}");
            }
            else report($"SQL_OPEN_FAIL; ExceptionType={HostedPortfolioStages.SafeTypeName(error.GetType())}");
            return 1;
        }
        finally
        {
            tokenValue = null;
            token = default; // Immutable token strings cannot be securely zeroed.
        }

        // Compare the original provider path only after the explicit token worked.
        try
        {
            await open(original, null, ct);
            report("SQL_PROVIDER_OPEN_PASS");
            return 0;
        }
        catch
        {
            report("SQL_PROVIDER_OPEN_FAIL");
            return 1;
        }
    }

    private static async Task OpenAsync(string connectionString, string? token, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        try
        {
            if (token is not null) connection.AccessToken = token;
            await connection.OpenAsync(ct);
        }
        finally
        {
            if (connection.State != System.Data.ConnectionState.Closed) await connection.CloseAsync();
            if (token is not null) connection.AccessToken = null;
        }
    }
}
