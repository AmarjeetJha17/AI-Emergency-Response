using EmergencyResponse.Application.DTOs.Incidents;

using FluentValidation;

namespace EmergencyResponse.Application.Validators.Incidents;

public sealed class CreateIncidentRequestValidator
    : AbstractValidator<CreateIncidentRequest>
{
    public CreateIncidentRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MinimumLength(10)
            .MaximumLength(5000);

        RuleFor(x => x.IncidentTypeId)
            .NotEmpty();

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180);

        RuleFor(x => x.Address)
            .MaximumLength(500);

        RuleFor(x => x.City)
            .MaximumLength(100);

        RuleFor(x => x.District)
            .MaximumLength(100);

        RuleFor(x => x.State)
            .MaximumLength(100);

        RuleFor(x => x.PeopleAffected)
            .GreaterThanOrEqualTo(0)
            .When(x => x.PeopleAffected.HasValue);

        RuleFor(x => x.VehiclesAffected)
            .GreaterThanOrEqualTo(0)
            .When(x => x.VehiclesAffected.HasValue);

        RuleFor(x => x.Casualties)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Casualties.HasValue);
    }
}