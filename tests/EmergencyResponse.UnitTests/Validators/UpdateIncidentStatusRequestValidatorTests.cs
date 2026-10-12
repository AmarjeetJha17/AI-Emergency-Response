using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Application.Validators.Incidents;

namespace EmergencyResponse.UnitTests.Validators;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class UpdateIncidentStatusRequestValidatorTests
{
    private readonly UpdateIncidentStatusRequestValidator _validator = new();

    [Theory]
    [InlineData("Submitted")]
    [InlineData("InProgress")]
    [InlineData("closed")]
    public void Validate_AcceptsKnownIncidentStatusesCaseInsensitively(string status)
    {
        var result = _validator.Validate(new UpdateIncidentStatusRequest { Status = status });

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(error => error.ErrorMessage)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("InFlightButNotAStatus")]
    public void Validate_RejectsMissingOrUnknownStatus(string status)
    {
        var result = _validator.Validate(new UpdateIncidentStatusRequest { Status = status });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateIncidentStatusRequest.Status));
        if (string.IsNullOrWhiteSpace(status))
        {
            Assert.Contains(result.Errors, error => error.ErrorMessage == "Status is required.");
        }
    }

    [Fact]
    public void Validate_AllowsNullRemarksAndAcceptsMaximumLengthRemarks()
    {
        var omitted = _validator.Validate(new UpdateIncidentStatusRequest { Status = "Resolved" });
        var atLimit = _validator.Validate(new UpdateIncidentStatusRequest
        {
            Status = "Resolved",
            Remarks = new string('R', 1000)
        });

        Assert.True(omitted.IsValid);
        Assert.True(atLimit.IsValid);
    }

    [Fact]
    public void Validate_RejectsRemarksLongerThan1000Characters()
    {
        var result = _validator.Validate(new UpdateIncidentStatusRequest
        {
            Status = "Resolved",
            Remarks = new string('R', 1001)
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateIncidentStatusRequest.Remarks));
    }
}
