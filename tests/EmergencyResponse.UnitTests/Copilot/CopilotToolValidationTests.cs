using EmergencyResponse.Application.Copilot.Tools;

namespace EmergencyResponse.UnitTests.Copilot;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class CopilotToolValidationTests
{
    [Fact]
    public void Empty_ids_are_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            CopilotToolValidation.RequiredId(Guid.Empty, "incidentId"));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-5, 10)]
    [InlineData(100, 20)]
    public void Limits_are_bounded(int requested, int expected)
    {
        Assert.Equal(expected, CopilotToolValidation.Take(requested, 20));
    }

    [Fact]
    public void Result_envelope_marks_data_as_available()
    {
        var result = CopilotToolResult<string>.Available("current");

        Assert.True(result.Success);
        Assert.True(result.DataAvailable);
        Assert.Equal("current", result.Data);
        Assert.NotEqual(default, result.RetrievedAtUtc);
    }

    [Fact]
    public void Execution_context_keeps_server_derived_security_data()
    {
        var context = new CopilotExecutionContext();
        var interactionId = Guid.NewGuid();
        context.Set(
            "operator-1",
            ["Operator"],
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            interactionId);

        Assert.True(context.IsAuthenticated);
        Assert.Equal("operator-1", context.UserId);
        Assert.Equal(interactionId, context.InteractionId);
        Assert.Contains("Operator", context.Roles);
    }
}
