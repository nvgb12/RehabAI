namespace RehabAI.Domain.Enums;

public enum AppointmentStatus
{
    PendingPayment = 1,
    Expired = 2,
    Pending = 3,
    Confirmed = 4,
    Completed = 5,
    Cancelled = 6,
    NoShow = 7,
    Requested = 8,
    Rejected = 9
}

public enum ScheduleSlotStatus
{
    Available = 1,
    SoftReserved = 2,
    Booked = 3,
    Disabled = 4
}
