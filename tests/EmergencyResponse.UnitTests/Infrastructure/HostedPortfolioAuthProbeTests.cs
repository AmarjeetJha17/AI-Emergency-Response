using Azure.Core;
using EmergencyResponse.Infrastructure.Persistence.Seed;
using Microsoft.Data.SqlClient;

namespace EmergencyResponse.UnitTests.Infrastructure;

public sealed class HostedPortfolioAuthProbeTests
{
    private const string Connection = "Server=sql-er-demo-qqgqkekj.database.windows.net;Database=EmergencyResponse;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;User ID=synthetic-identity";
    private const string Secret = "Password=secret-marker;AccessToken=token-marker;SELECT private_statement";

    [Fact]
    public void ExplicitTokenSettingsRemoveAuthenticationWithoutWeakeningEncryption()
    {
        var parsed = new SqlConnectionStringBuilder(HostedPortfolioAuthProbe.ExplicitTokenConnection(Connection));
        Assert.Equal(SqlAuthenticationMethod.NotSpecified, parsed.Authentication);
        Assert.Empty(parsed.UserID);
        Assert.Empty(parsed.Password);
        Assert.False(parsed.IntegratedSecurity);
        Assert.False(parsed.TrustServerCertificate);
        Assert.Equal(SqlConnectionEncryptOption.Mandatory, parsed.Encrypt);
        Assert.False(parsed.Pooling);
        Assert.Equal(HostedPortfolioSeeder.AzureServer, parsed.DataSource);
        Assert.Equal("EmergencyResponse", parsed.InitialCatalog);
    }

    [Theory]
    [InlineData("Server=other.database.windows.net;Database=EmergencyResponse;Authentication=Active Directory Default")]
    [InlineData("Server=sql-er-demo-qqgqkekj.database.windows.net;Database=master;Authentication=Active Directory Default")]
    [InlineData("Server=sql-er-demo-qqgqkekj.database.windows.net;Database=EmergencyResponse;Authentication=Active Directory Default;TrustServerCertificate=True")]
    public void UnsafeTargetsAreRejected(string connection) =>
        Assert.Throws<HostedPortfolioException>(() => HostedPortfolioAuthProbe.ExplicitTokenConnection(connection));

    [Fact]
    public async Task SameObtainedTokenIsUsedThenOriginalProviderPathIsTested()
    {
        var credential = new FakeCredential();
        var output = new List<string>();
        var calls = new List<(string, string?)>();
        var result = await HostedPortfolioAuthProbe.RunAsync(Connection, credential, output.Add,
            open: (connection, token, _) => { calls.Add((connection, token)); return Task.CompletedTask; });
        Assert.Equal(0, result);
        Assert.Equal(HostedPortfolioAuthProbe.SqlScope, credential.Scope);
        Assert.Equal(Secret, calls[0].Item2);
        Assert.Equal(Connection, calls[1].Item1);
        Assert.Null(calls[1].Item2);
        Assert.Equal(new[] { "TOKEN_PASS", "SQL_OPEN_PASS", "SQL_PROVIDER_OPEN_PASS" }, output);
    }

    [Fact]
    public async Task TokenFailureNeverOpensSqlOrPrintsCredentialMessages()
    {
        var output = new List<string>();
        var result = await HostedPortfolioAuthProbe.RunAsync(Connection, new FakeCredential(fail: true), output.Add,
            open: (_, _, _) => throw new Xunit.Sdk.XunitException("SQL must not open"));
        Assert.Equal(1, result);
        Assert.Equal(new[] { "TOKEN_FAIL; ExceptionType=InvalidOperationException" }, output);
        AssertSafe(output);
    }

    [Fact]
    public async Task ExplicitOpenFailureStopsBeforeProviderComparison()
    {
        var output = new List<string>();
        var calls = 0;
        var result = await HostedPortfolioAuthProbe.RunAsync(Connection, new FakeCredential(), output.Add,
            open: (_, _, _) => { calls++; throw new InvalidOperationException(Secret); });
        Assert.Equal(1, result);
        Assert.Equal(1, calls);
        Assert.Equal(new[] { "TOKEN_PASS", "SQL_OPEN_FAIL; ExceptionType=InvalidOperationException" }, output);
        AssertSafe(output);
    }

    [Fact]
    public async Task ProviderFailurePrintsOnlyItsStatus()
    {
        var output = new List<string>();
        var calls = 0;
        var result = await HostedPortfolioAuthProbe.RunAsync(Connection, new FakeCredential(), output.Add,
            open: (_, _, _) => ++calls == 1 ? Task.CompletedTask : throw new InvalidOperationException(Secret));
        Assert.Equal(1, result);
        Assert.Equal(new[] { "TOKEN_PASS", "SQL_OPEN_PASS", "SQL_PROVIDER_OPEN_FAIL" }, output);
        AssertSafe(output);
    }

    private static void AssertSafe(IEnumerable<string> output)
    {
        var text = string.Join(" ", output);
        foreach (var fragment in new[] { "secret-marker", "token-marker", "Password=", "AccessToken=", "SELECT", "private_statement", "Server=" })
            Assert.DoesNotContain(fragment, text);
    }

    private sealed class FakeCredential(bool fail = false) : TokenCredential
    {
        internal string? Scope { get; private set; }
        public override AccessToken GetToken(TokenRequestContext context, CancellationToken cancellationToken)
        {
            Scope = Assert.Single(context.Scopes);
            if (fail) throw new InvalidOperationException(Secret);
            return new AccessToken(Secret, DateTimeOffset.UtcNow.AddMinutes(5));
        }
        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext context, CancellationToken cancellationToken)
            => ValueTask.FromResult(GetToken(context, cancellationToken));
    }
}
