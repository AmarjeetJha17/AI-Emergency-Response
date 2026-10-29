namespace EmergencyResponse.Infrastructure.Persistence.Seed;

public enum HostedPortfolioStage
{
    Configuration = 1, Context = 2, OpenConnection = 3, VerifyTarget = 4,
    ReferenceData = 5, Ownership = 6, Dataset = 7, Serialization = 8,
    Inventory = 9, Confirmation = 10, Write = 11
}

public sealed class HostedPortfolioStages(Action<string>? report = null)
{
    public HostedPortfolioStage Current { get; private set; } = HostedPortfolioStage.Configuration;
    public static string Code(HostedPortfolioStage stage) => stage switch
    {
        HostedPortfolioStage.Configuration => "ST001", HostedPortfolioStage.Context => "ST002",
        HostedPortfolioStage.OpenConnection => "ST003", HostedPortfolioStage.VerifyTarget => "ST004",
        HostedPortfolioStage.ReferenceData => "ST005", HostedPortfolioStage.Ownership => "ST006",
        HostedPortfolioStage.Dataset => "ST007", HostedPortfolioStage.Serialization => "ST008",
        HostedPortfolioStage.Inventory => "ST009", HostedPortfolioStage.Confirmation => "ST010",
        HostedPortfolioStage.Write => "ST011", _ => "ST000"
    };
    public void Enter(HostedPortfolioStage stage)
    {
        Current = stage;
        report?.Invoke($"{Code(stage)} started");
    }
    public void Complete() => report?.Invoke($"{Code(Current)} completed");

    public string Failure(Exception error)
    {
        if (error is Microsoft.Data.SqlClient.SqlException sql)
        {
            // At most eight error triples, plus fixed-size scalar metadata.
            // Do not enumerate SqlError messages or call exception.ToString().
            var triples = new List<string>();
            for (var i = 0; i < Math.Min(sql.Errors.Count, 8); i++)
            {
                var item = sql.Errors[i];
                triples.Add(FormattableString.Invariant($"Number={item.Number},State={item.State},Class={item.Class}"));
            }
            var inner = sql.InnerException is { } immediate ? SafeTypeName(immediate.GetType()) : "None";
            return $"Stage={Code(Current)}; SqlErrors=[{string.Join("|", triples)}]; HResult=0x{unchecked((uint)sql.HResult):X8}; InnerExceptionType={inner}";
        }
        var diagnostic = HostedPortfolioDiagnostics.Classify(error);
        if (diagnostic.Code != "HP999") return $"{diagnostic}; Stage={Code(Current)}";
        // Names of framework/provider types only, never messages or full type
        // descriptions. Custom exception names cannot inject arbitrary output.
        return $"HP999; Stage={Code(Current)}; ExceptionType={SafeTypeName(error.GetType())}";
    }

    public static string SafeTypeName(Type type) => type.Namespace is { } ns &&
            (ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal) || ns.StartsWith("Microsoft.Data.", StringComparison.Ordinal))
            && System.Text.RegularExpressions.Regex.IsMatch(type.Name, "^[A-Za-z][A-Za-z0-9]{0,79}$")
            ? type.Name : "Exception";
}
