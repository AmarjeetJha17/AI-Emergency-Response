using EmergencyResponse.Infrastructure.Persistence.Seed;

namespace EmergencyResponse.UnitTests.Infrastructure;

public sealed class HostedPortfolioPrincipalSidTests
{
    [Fact]
    public void ContainedMemberUserObjectIdHasAcceptedSixteenByteRepresentation()
    {
        // Synthetic GUID, not a tenant identity. SQL uniqueidentifier/binary(16)
        // and Guid.ToByteArray use this representation; use SQL's actual sid for
        // operator approval instead of manually removing GUID punctuation.
        var sid = "0x" + Convert.ToHexString(Guid.Parse("12345678-1234-5678-9012-123456789012").ToByteArray());
        Assert.Equal(34, sid.Length);
        Assert.Equal(sid, HostedPortfolioSessionVerifier.ValidateExpectedSid(sid));
        HostedPortfolioSessionVerifier.Verify(new HostedPortfolioSession(sid, "E", true, true), sid);
    }

    [Fact]
    public void ThirtyTwoByteDboSidIsNotAnApprovedExternalUserSid()
    {
        Assert.Throws<HostedPortfolioException>(() => HostedPortfolioSessionVerifier.ValidateExpectedSid("0x" + new string('A', 64)));
        var valid = "0x" + new string('A', 32);
        Assert.Throws<HostedPortfolioException>(() => HostedPortfolioSessionVerifier.Verify(
            new HostedPortfolioSession(valid, "S", true, true), valid));
        Assert.Throws<HostedPortfolioException>(() => HostedPortfolioSessionVerifier.Verify(
            new HostedPortfolioSession(valid, "X", true, true), valid));
        Assert.Throws<HostedPortfolioException>(() => HostedPortfolioSessionVerifier.Verify(
            new HostedPortfolioSession(valid, "E", false, true), valid));
    }
}
