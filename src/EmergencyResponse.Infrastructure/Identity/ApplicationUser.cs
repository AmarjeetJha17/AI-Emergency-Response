using Microsoft.AspNetCore.Identity;

namespace EmergencyResponse.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public EmergencyResponse.Domain.Enums.DataClassification DataClassification { get; set; } = EmergencyResponse.Domain.Enums.DataClassification.Restricted;
    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public Guid? DepartmentId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }
}
