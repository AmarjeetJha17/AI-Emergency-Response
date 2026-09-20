using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Domain.Enums;

using FluentValidation;

namespace EmergencyResponse.Application.Validators.Incidents;

public sealed class UpdateIncidentStatusRequestValidator
    : AbstractValidator<UpdateIncidentStatusRequest>
{
    private static readonly string[] ValidStatuses =
        Enum.GetNames<IncidentStatus>();

    public UpdateIncidentStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("Status is required.")
            .Must(s => ValidStatuses.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage(
                $"Status must be one of: {string.Join(", ", ValidStatuses)}.");

        RuleFor(x => x.Remarks)
            .MaximumLength(1000)
            .When(x => x.Remarks is not null);
    }
}
