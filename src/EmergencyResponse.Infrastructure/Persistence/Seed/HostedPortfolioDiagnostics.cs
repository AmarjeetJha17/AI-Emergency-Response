using System.Net.Sockets;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Infrastructure.Persistence.Seed;

public enum HostedPortfolioFailure
{
    Configuration, SqlAuthorization, SqlConnection, Target, ReferenceData, Ownership, Integrity, Unknown,
    Confirmation, Approval, Principal, Permissions
}

/// <summary>Only explicit seeder checks may assign these application categories.</summary>
public sealed class HostedPortfolioException(HostedPortfolioFailure failure) : InvalidOperationException
{
    public HostedPortfolioFailure Failure { get; } = failure;
}

public sealed record HostedPortfolioDiagnostic(string Code, string Category, string Action)
{
    public override string ToString() => $"{Code}; {Category}; {Action}";
}

public static class HostedPortfolioDiagnostics
{
    // Fixed output only: never interpolate exception properties or caller input.
    public static HostedPortfolioDiagnostic Describe(HostedPortfolioFailure failure) => failure switch
    {
        HostedPortfolioFailure.Configuration => new("HP001", "Missing or invalid connection configuration", "Check PORTFOLIO_SEED_CONNECTION and supported connection options."),
        HostedPortfolioFailure.SqlAuthorization => new("HP002", "SQL authentication or authorization failure", "Verify the Entra identity and existing database access through an approved channel."),
        HostedPortfolioFailure.SqlConnection => new("HP003", "SQL networking or connection failure", "Check the approved SQL network path and server availability."),
        HostedPortfolioFailure.Target => new("HP004", "Wrong database or server target", "Use the documented allowlisted server and database."),
        HostedPortfolioFailure.ReferenceData => new("HP005", "Missing reference data", "Verify required active incident types; this tool does not create reference data."),
        HostedPortfolioFailure.Ownership => new("HP006", "Dataset ownership conflict", "Review reserved identifiers and ownership evidence before proceeding."),
        HostedPortfolioFailure.Integrity => new("HP007", "Dataset integrity or idempotency violation", "Review dataset completeness and constraints; do not repair by blindly reseeding."),
        HostedPortfolioFailure.Confirmation => new("HP008", "Write authorization not confirmed", "Obtain separate authorization and use the interactive confirmation."),
        HostedPortfolioFailure.Approval => new("HP009", "Preview approval mismatch", "Review a fresh preview and explicitly approve its fingerprint."),
        HostedPortfolioFailure.Principal => new("HP010", "SQL principal mismatch", "Verify the approved direct Entra database principal through an authorized channel."),
        HostedPortfolioFailure.Permissions => new("HP011", "Required SQL permissions unverified", "Have an authorized administrator review the documented minimum grants; no grants were changed."),
        _ => new("HP999", "Unknown failure", "Stop and investigate through an authorized diagnostic channel; do not retry automatically.")
    };

    public static HostedPortfolioFailure ClassifySqlNumber(int number) => number switch
    {
        // 4060 can mean a missing database OR insufficient access. It cannot
        // establish the root cause, so keep it in the access category.
        18456 or 18452 or 4060 or 916 or 229 or 230 or 262 or 297 => HostedPortfolioFailure.SqlAuthorization,
        -2 or 2 or 20 or 26 or 40 or 53 or 64 or 121 or 233 or 258 or 10053 or 10054 or 10060 or 10061 or 11001
            => HostedPortfolioFailure.SqlConnection,
        2601 or 2627 or 547 => HostedPortfolioFailure.Integrity,
        _ => HostedPortfolioFailure.Unknown
    };

    public static HostedPortfolioDiagnostic Classify(Exception exception)
        => Classify(exception, 0);

    public static HostedPortfolioDiagnostic DescribeSqlNumber(int number)
    {
        var diagnostic = Describe(ClassifySqlNumber(number));
        // An integer SQL error number is safe metadata, including when unknown.
        // Never include SQL messages, procedure names or statement text.
        return diagnostic with { Action = $"SQL error number {number.ToString(System.Globalization.CultureInfo.InvariantCulture)}. {diagnostic.Action}" };
    }

    private static HostedPortfolioDiagnostic Classify(Exception exception, int depth)
    {
        if (depth >= 4) return Describe(HostedPortfolioFailure.Unknown);
        // Only traverse wrappers whose purpose is to contain the actual failure.
        // Do not walk arbitrary inner exceptions or emit any of their properties.
        if (exception is TypeInitializationException or System.Reflection.TargetInvocationException)
            return exception.InnerException is { } inner
                ? Classify(inner, depth + 1) : Describe(HostedPortfolioFailure.Unknown);
        if (exception is AggregateException { InnerExceptions.Count: 1 } aggregate)
            return Classify(aggregate.InnerExceptions[0], depth + 1);
        if (exception is SqlException sqlError) return DescribeSqlNumber(sqlError.Number);
        if (exception is DbUpdateException { InnerException: SqlException sqlInner })
            return DescribeSqlNumber(sqlInner.Number);
        // These remain unknown failures, but fixed type-based actions identify
        // which local subsystem to investigate without printing its message.
        if (exception is not HostedPortfolioException)
        {
            var action = exception switch
            {
                InvalidOperationException => "Investigate client or EF configuration (InvalidOperationException) through an authorized channel.",
                NotSupportedException => "Investigate provider or runtime compatibility (NotSupportedException) through an authorized channel.",
                ArgumentException => "Investigate client parameter validation (ArgumentException) through an authorized channel.",
                System.IO.FileNotFoundException or System.IO.FileLoadException or BadImageFormatException
                    => "Investigate runtime assembly loading through an authorized channel.",
                TypeLoadException or MissingMethodException => "Investigate runtime dependency compatibility through an authorized channel.",
                _ => null
            };
            if (action is not null) return new("HP999", "Unknown failure", action);
        }
        var failure = exception switch
        {
            HostedPortfolioException expected => expected.Failure,
            AuthenticationFailedException or CredentialUnavailableException => HostedPortfolioFailure.SqlAuthorization,
            SocketException => HostedPortfolioFailure.SqlConnection,
            _ => HostedPortfolioFailure.Unknown
        };
        return Describe(failure);
    }
}
