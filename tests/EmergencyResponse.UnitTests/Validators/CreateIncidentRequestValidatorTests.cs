using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Application.Validators.Incidents;

namespace EmergencyResponse.UnitTests.Validators;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class CreateIncidentRequestValidatorTests
{
    private readonly CreateIncidentRequestValidator _validator = new();

    [Fact]
    public void Validate_WhenAllRequiredAndOptionalFieldsAreWithinLimits_IsValid()
    {
        var request = ValidRequest() with
        {
            Title = new string('T', 200),
            Description = new string('D', 10),
            Latitude = -90,
            Longitude = 180,
            Address = new string('A', 500),
            City = new string('C', 100),
            District = new string('D', 100),
            State = new string('S', 100),
            PeopleAffected = 0,
            VehiclesAffected = 0,
            Casualties = 0
        };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(error => error.ErrorMessage)));
    }

    [Fact]
    public void Validate_WhenRequiredFieldsAreMissing_ReportsEachRequiredProperty()
    {
        var request = ValidRequest() with
        {
            Title = null!,
            Description = string.Empty,
            IncidentTypeId = Guid.Empty
        };

        var result = _validator.Validate(request);

        AssertInvalid(result, nameof(CreateIncidentRequest.Title));
        AssertInvalid(result, nameof(CreateIncidentRequest.Description));
        AssertInvalid(result, nameof(CreateIncidentRequest.IncidentTypeId));
    }

    [Fact]
    public void Validate_WhenTextFieldsExceedMaximumLengths_RejectsEachField()
    {
        var request = ValidRequest() with
        {
            Title = new string('T', 201),
            Description = new string('D', 5001),
            Address = new string('A', 501),
            City = new string('C', 101),
            District = new string('D', 101),
            State = new string('S', 101)
        };

        var result = _validator.Validate(request);

        AssertInvalid(result, nameof(CreateIncidentRequest.Title));
        AssertInvalid(result, nameof(CreateIncidentRequest.Description));
        AssertInvalid(result, nameof(CreateIncidentRequest.Address));
        AssertInvalid(result, nameof(CreateIncidentRequest.City));
        AssertInvalid(result, nameof(CreateIncidentRequest.District));
        AssertInvalid(result, nameof(CreateIncidentRequest.State));
    }

    [Theory]
    [InlineData(-90.01, 0)]
    [InlineData(90.01, 0)]
    [InlineData(0, -180.01)]
    [InlineData(0, 180.01)]
    public void Validate_WhenCoordinatesFallOutsideGeographicRanges_RejectsCoordinate(
        double latitude,
        double longitude)
    {
        var request = ValidRequest() with
        {
            Latitude = (decimal)latitude,
            Longitude = (decimal)longitude
        };

        var result = _validator.Validate(request);

        if (latitude is < -90 or > 90)
        {
            AssertInvalid(result, nameof(CreateIncidentRequest.Latitude));
        }
        if (longitude is < -180 or > 180)
        {
            AssertInvalid(result, nameof(CreateIncidentRequest.Longitude));
        }
    }

    [Fact]
    public void Validate_WhenOptionalCountsAreNegative_RejectsEachCount()
    {
        var request = ValidRequest() with
        {
            PeopleAffected = -1,
            VehiclesAffected = -1,
            Casualties = -1
        };

        var result = _validator.Validate(request);

        AssertInvalid(result, nameof(CreateIncidentRequest.PeopleAffected));
        AssertInvalid(result, nameof(CreateIncidentRequest.VehiclesAffected));
        AssertInvalid(result, nameof(CreateIncidentRequest.Casualties));
    }

    [Fact]
    public void Validate_WhenOptionalFieldsAreNull_AllowsOmittedValues()
    {
        var result = _validator.Validate(ValidRequest());

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(error => error.ErrorMessage)));
    }

    private static CreateIncidentRequest ValidRequest()
        => new()
        {
            Title = "Warehouse fire",
            Description = "Smoke visible near the loading bay.",
            IncidentTypeId = Guid.NewGuid(),
            Latitude = 28.6m,
            Longitude = 77.2m
        };

    private static void AssertInvalid(
        FluentValidation.Results.ValidationResult result,
        string propertyName)
    {
        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
    }
}
