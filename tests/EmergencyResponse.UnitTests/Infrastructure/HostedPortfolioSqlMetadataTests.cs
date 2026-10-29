using System.Reflection;
using EmergencyResponse.Infrastructure.Persistence.Seed;
using Microsoft.Data.SqlClient;

namespace EmergencyResponse.UnitTests.Infrastructure;

public sealed class HostedPortfolioSqlMetadataTests
{
    private const string Secret = "Password=secret-marker;AccessToken=token-marker;Server=private-server;SELECT private_statement";

    [Fact]
    public void SqlFailureEmitsOnlyNumericMetadataAndImmediateType()
    {
        var error = CreateSqlException(2, new InvalidOperationException(Secret, new Exception(Secret)));
        error.Data["credential"] = Secret;
        var stages = new HostedPortfolioStages();
        stages.Enter(HostedPortfolioStage.OpenConnection);
        Assert.Equal($"Stage=ST003; SqlErrors=[Number=0,State=7,Class=20|Number=1,State=7,Class=20]; HResult=0x{unchecked((uint)error.HResult):X8}; InnerExceptionType=InvalidOperationException", stages.Failure(error));
        AssertSafe(stages.Failure(error));
    }

    [Fact]
    public void LargeSqlErrorCollectionsAreBoundedAndNullInnerIsSupported()
    {
        var stages = new HostedPortfolioStages();
        stages.Enter(HostedPortfolioStage.OpenConnection);
        var output = stages.Failure(CreateSqlException(100, null));
        Assert.Equal(8, output.Split("Number=").Length - 1);
        Assert.DoesNotContain("Number=8,", output);
        Assert.EndsWith("InnerExceptionType=None", output);
        Assert.True(output.Length < 1024);
        AssertSafe(output);
    }

    private static void AssertSafe(string output)
    {
        foreach (var fragment in new[] { "Password", "secret-marker", "AccessToken", "token-marker", "Server=", "private-server", "SELECT", "private_statement", "StackTrace" })
            Assert.DoesNotContain(fragment, output);
        Assert.DoesNotContain('\n', output);
        Assert.DoesNotContain('\r', output);
    }

    // SqlException/SqlError have no public constructors. Construct real provider
    // instances in tests only; no SQL connection or server operation is involved.
    private static SqlException CreateSqlException(int count, Exception? inner)
    {
        var collection = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)!;
        var add = typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var errorConstructor = typeof(SqlError).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .First(x => x.GetParameters().Any(p => p.Name == "infoNumber"));
        for (var i = 0; i < count; i++)
        {
            var arguments = errorConstructor.GetParameters().Select(p =>
                p.Name == "infoNumber" ? (object)i :
                p.Name == "errorState" ? (byte)7 :
                p.Name == "errorClass" ? (byte)20 :
                p.ParameterType == typeof(string) ? Secret :
                p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null).ToArray();
            add.Invoke(collection, [errorConstructor.Invoke(arguments)]);
        }
        var constructor = typeof(SqlException).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .First(x => x.GetParameters().Any(p => p.ParameterType == typeof(SqlErrorCollection)));
        return (SqlException)constructor.Invoke(constructor.GetParameters().Select(p =>
            p.ParameterType == typeof(SqlErrorCollection) ? (object)collection :
            p.ParameterType == typeof(string) ? Secret :
            p.ParameterType == typeof(Exception) ? inner :
            p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null).ToArray());
    }
}
