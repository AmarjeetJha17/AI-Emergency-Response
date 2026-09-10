using EmergencyResponse.Domain.Common;

namespace EmergencyResponse.Domain.Entities;

public class Notification : BaseEntity
{
    public string UserId { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Type { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime? ReadAtUtc { get; set; }
}