using System.Net.Sockets;
using Azure.Identity;
using EmergencyResponse.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.UnitTests.Infrastructure;

public sealed class HostedPortfolioDiagnosticsTests
{
    private const string Sensitive = "Password=secret-marker;Server=private-marker;User ID=private-user;AccessToken=token-marker;SELECT confidential FROM private_table";

    [Fact]
    public void EveryCategoryEmitsOnlyPredefinedFieldsEvenWithSensitiveMetadata()
    {
        foreach (var failure in Enum.GetValues<HostedPortfolioFailure>())
        {
            var exception = new HostedPortfolioException(failure);
            exception.Data["connection"] = Sensitive;
            var diagnostic = HostedPortfolioDiagnostics.Classify(exception);
            Assert.Equal(HostedPortfolioDiagnostics.Describe(failure), diagnostic);
            AssertSafe(diagnostic.ToString());
        }
        Assert.Equal("HP999", HostedPortfolioDiagnostics.Classify(new HostedPortfolioException((HostedPortfolioFailure)999)).Code);
    }

    [Fact]
    public void ArbitraryMessagesAndInnerMessagesNeverDetermineClassificationOrOutput()
    {
        Exception[] failures =
        [
            new Exception(Sensitive),
            new InvalidOperationException("Login failed 18456 " + Sensitive),
            new ArgumentException(Sensitive),
            new DbUpdateException(Sensitive, new Exception(Sensitive)),
            new Exception(Sensitive, new HostedPortfolioException(HostedPortfolioFailure.Ownership))
        ];
        foreach (var error in failures)
        {
            var diagnostic = HostedPortfolioDiagnostics.Classify(error);
            Assert.Equal("HP999", diagnostic.Code);
            AssertSafe(diagnostic.ToString());
        }
    }

    [Fact]
    public void KnownWrappersExposeOnlySafeClassificationWithBoundedTraversal()
    {
        var expected = new HostedPortfolioException(HostedPortfolioFailure.ReferenceData);
        Exception wrapper = new TypeInitializationException(Sensitive,
            new System.Reflection.TargetInvocationException(Sensitive, expected));
        Assert.Equal("HP005", HostedPortfolioDiagnostics.Classify(wrapper).Code);
        AssertSafe(HostedPortfolioDiagnostics.Classify(wrapper).ToString());
        Assert.Equal("HP005", HostedPortfolioDiagnostics.Classify(new AggregateException(Sensitive, expected)).Code);
        Assert.Equal("HP999", HostedPortfolioDiagnostics.Classify(new AggregateException(expected, expected)).Code);
        for (var i = 0; i < 5; i++) wrapper = new TypeInitializationException(Sensitive, wrapper);
        Assert.Equal("HP999", HostedPortfolioDiagnostics.Classify(wrapper).Code);
    }

    [Theory]
    [InlineData(18456, "HP002")]
    [InlineData(40615, "HP999")]
    [InlineData(-2146893019, "HP999")]
    public void SqlNumberIsVisibleWithoutSqlMessageOrSensitiveProperties(int number, string code)
    {
        var diagnostic = HostedPortfolioDiagnostics.DescribeSqlNumber(number);
        Assert.Equal(code, diagnostic.Code);
        Assert.Contains(number.ToString(System.Globalization.CultureInfo.InvariantCulture), diagnostic.Action);
        AssertSafe(diagnostic.ToString());
    }

    [Fact]
    public void UnknownTypeFamiliesProvideOnlyFixedActions()
    {
        Exception[] failures =
        [
            new InvalidOperationException(Sensitive), new NotSupportedException(Sensitive),
            new ArgumentException(Sensitive), new System.IO.FileNotFoundException(Sensitive, Sensitive),
            new System.IO.FileLoadException(Sensitive, Sensitive), new BadImageFormatException(Sensitive),
            new TypeLoadException(Sensitive), new MissingMethodException(Sensitive)
        ];
        foreach (var error in failures)
        {
            var diagnostic = HostedPortfolioDiagnostics.Classify(error);
            Assert.Equal("HP999", diagnostic.Code);
            Assert.NotEqual(HostedPortfolioDiagnostics.Describe(HostedPortfolioFailure.Unknown).Action, diagnostic.Action);
            AssertSafe(diagnostic.ToString());
        }
    }

    [Fact]
    public void KnownCredentialAndNetworkTypesDoNotExposeTheirMessages()
    {
        Assert.Equal("HP002", HostedPortfolioDiagnostics.Classify(new AuthenticationFailedException(Sensitive)).Code);
        Assert.Equal("HP002", HostedPortfolioDiagnostics.Classify(new CredentialUnavailableException(Sensitive)).Code);
        Assert.Equal("HP003", HostedPortfolioDiagnostics.Classify(new SocketException()).Code);
        AssertSafe(HostedPortfolioDiagnostics.Classify(new AuthenticationFailedException(Sensitive)).ToString());
    }

    [Theory]
    [InlineData(18456, "HP002")]
    [InlineData(4060, "HP002")]
    [InlineData(229, "HP002")]
    [InlineData(-2, "HP003")]
    [InlineData(53, "HP003")]
    [InlineData(10060, "HP003")]
    [InlineData(2627, "HP007")]
    [InlineData(547, "HP007")]
    [InlineData(99999, "HP999")]
    public void SqlNumbersUseStableAllowlist(int number, string code)
    {
        var diagnostic = HostedPortfolioDiagnostics.Describe(HostedPortfolioDiagnostics.ClassifySqlNumber(number));
        Assert.Equal(code, diagnostic.Code);
        AssertSafe(diagnostic.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("Password=secret-marker;unknown-option=token-marker")]
    public void MalformedConfigurationIsSanitizedAtParseBoundary(string connection)
    {
        var exception = Assert.Throws<HostedPortfolioException>(() => HostedPortfolioSeeder.ValidateConnection(connection));
        Assert.Equal("HP001", HostedPortfolioDiagnostics.Classify(exception).Code);
        AssertSafe(HostedPortfolioDiagnostics.Classify(exception).ToString());
    }

    private static void AssertSafe(string output)
    {
        foreach (var fragment in new[] { "secret-marker", "private-marker", "private-user", "token-marker", "Password=", "Server=", "User ID=", "AccessToken=", "SELECT", "private_table", "StackTrace" })
            Assert.DoesNotContain(fragment, output);
        Assert.DoesNotContain('\n', output);
        Assert.DoesNotContain('\r', output);
    }
}
