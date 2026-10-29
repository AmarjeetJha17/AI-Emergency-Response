namespace EmergencyResponse.Infrastructure.Persistence.Seed;

public sealed record HostedPortfolioApprovalInput(string ExpectedPrincipalSid, string? ExpectedFingerprint)
{
    public static HostedPortfolioApprovalInput Parse(string[] args)
    {
        if (args.Length == 0 || args[0] is not ("hosted-preview" or "hosted-seed"))
            throw new HostedPortfolioException(HostedPortfolioFailure.Configuration);
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 1; i < args.Length; i += 2)
        {
            if (i + 1 >= args.Length || args[i] is not ("--expected-principal-sid" or "--expected-fingerprint") ||
                !options.TryAdd(args[i], args[i + 1]))
                throw new HostedPortfolioException(HostedPortfolioFailure.Configuration);
        }
        options.TryGetValue("--expected-principal-sid", out var sid);
        sid = HostedPortfolioSessionVerifier.ValidateExpectedSid(sid);
        options.TryGetValue("--expected-fingerprint", out var fingerprint);
        if (args[0] == "hosted-seed") HostedPortfolioSeeder.ValidateFingerprint(fingerprint);
        else if (fingerprint is not null) throw new HostedPortfolioException(HostedPortfolioFailure.Configuration);
        return new(sid, fingerprint);
    }
}
