namespace RehabAI.Domain.Enums;

public enum AccountStatus
{
    Unverified = 1,
    PendingEmail = 2,
    PendingPasswordSetup = 3,
    Active = 4,
    Locked = 5,
    Suspended = 6,
    Deactivated = 7
}
