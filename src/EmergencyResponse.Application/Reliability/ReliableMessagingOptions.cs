namespace EmergencyResponse.Application.Reliability;

public sealed class ReliableMessagingOptions
{
    public const string SectionName = "Messaging:Retry";
    public int MaximumDeliveryAttempts { get; set; } = 8;
    public int[] BackoffSeconds { get; set; } = [2, 5, 15, 60];
    public int JitterMilliseconds { get; set; } = 750;
}
