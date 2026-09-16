namespace EmergencyResponse.Domain.Enums;

public enum DuplicateResolution
{
    Pending = 1,
    ConfirmedDuplicate = 2,
    NotDuplicate = 3,
    LinkedToMaster = 4
}