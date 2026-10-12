using EmergencyResponse.Application.DTOs.Authentication;
using EmergencyResponse.Application.Validators.Authentication;

namespace EmergencyResponse.UnitTests.Validators;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class AuthenticationRequestValidatorTests
{
    [Fact]
    public void RegisterRequestValidator_AcceptsValidRequestAtOptionalFieldLimits()
    {
        var request = ValidRegistration() with
        {
            FirstName = new string('F', 100),
            LastName = new string('L', 100),
            PhoneNumber = new string('1', 20)
        };

        var result = new RegisterRequestValidator().Validate(request);

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(error => error.ErrorMessage)));
    }

    [Fact]
    public void RegisterRequestValidator_ReportsMissingRequiredFields()
    {
        var request = ValidRegistration() with
        {
            FirstName = " ",
            LastName = string.Empty,
            Email = string.Empty,
            Password = string.Empty
        };

        var result = new RegisterRequestValidator().Validate(request);

        AssertInvalid(result, nameof(RegisterRequest.FirstName));
        AssertInvalid(result, nameof(RegisterRequest.LastName));
        AssertInvalid(result, nameof(RegisterRequest.Email));
        AssertInvalid(result, nameof(RegisterRequest.Password));
    }

    [Fact]
    public void RegisterRequestValidator_RejectsInvalidEmailAndOverlongFields()
    {
        var request = ValidRegistration() with
        {
            FirstName = new string('F', 101),
            LastName = new string('L', 101),
            Email = $"{new string('a', 252)}@b.com",
            PhoneNumber = new string('1', 21)
        };
        var invalidEmail = ValidRegistration() with { Email = "not-an-email" };

        var result = new RegisterRequestValidator().Validate(request);
        var invalidEmailResult = new RegisterRequestValidator().Validate(invalidEmail);

        AssertInvalid(result, nameof(RegisterRequest.FirstName));
        AssertInvalid(result, nameof(RegisterRequest.LastName));
        AssertInvalid(result, nameof(RegisterRequest.Email));
        AssertInvalid(result, nameof(RegisterRequest.PhoneNumber));
        AssertInvalid(invalidEmailResult, nameof(RegisterRequest.Email));
    }

    [Theory]
    [InlineData("Abc123!", "Password")]
    [InlineData("lowercase1!", "uppercase")]
    [InlineData("UPPERCASE1!", "lowercase")]
    [InlineData("Abcdefgh!", "digit")]
    [InlineData("Abcdefgh1", "special character")]
    public void RegisterRequestValidator_RejectsPasswordsThatMissLengthOrCompositionRules(
        string password,
        string expectedMessage)
    {
        var result = new RegisterRequestValidator().Validate(
            ValidRegistration() with { Password = password });

        AssertInvalid(result, nameof(RegisterRequest.Password));
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(RegisterRequest.Password) &&
            error.ErrorMessage.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RegisterRequestValidator_RejectsPasswordLongerThan128Characters()
    {
        var password = $"A{new string('a', 126)}1!";

        var result = new RegisterRequestValidator().Validate(
            ValidRegistration() with { Password = password });

        Assert.Equal(129, password.Length);
        AssertInvalid(result, nameof(RegisterRequest.Password));
    }

    [Fact]
    public void LoginRequestValidator_AcceptsValidCredentials()
    {
        var result = new LoginRequestValidator().Validate(
            new LoginRequest { Email = "operator@example.test", Password = "correct horse" });

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(error => error.ErrorMessage)));
    }

    [Fact]
    public void LoginRequestValidator_RejectsMissingMalformedOrOverlongCredentials()
    {
        var validator = new LoginRequestValidator();
        var missingResult = validator.Validate(new LoginRequest { Email = "", Password = "" });
        var malformedResult = validator.Validate(new LoginRequest { Email = "bad-email", Password = "valid" });
        var overlongResult = validator.Validate(new LoginRequest
        {
            Email = $"{new string('a', 251)}@b.com",
            Password = new string('p', 129)
        });

        AssertInvalid(missingResult, nameof(LoginRequest.Email));
        AssertInvalid(missingResult, nameof(LoginRequest.Password));
        AssertInvalid(malformedResult, nameof(LoginRequest.Email));
        AssertInvalid(overlongResult, nameof(LoginRequest.Email));
        AssertInvalid(overlongResult, nameof(LoginRequest.Password));
    }

    private static RegisterRequest ValidRegistration()
        => new()
        {
            FirstName = "Alex",
            LastName = "Operator",
            Email = "alex@example.test",
            Password = "StrongPass1!"
        };

    private static void AssertInvalid(
        FluentValidation.Results.ValidationResult result,
        string propertyName)
    {
        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
    }
}
