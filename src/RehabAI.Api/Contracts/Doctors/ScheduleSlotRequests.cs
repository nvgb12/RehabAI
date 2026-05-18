namespace RehabAI.Api.Contracts.Doctors;

public sealed record CreateDoctorScheduleSlotRequest(
    DateTimeOffset StartTime,
    DateTimeOffset EndTime);

public sealed record UpdateDoctorScheduleSlotRequest(
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string Status);
