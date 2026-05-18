namespace RehabAI.Api.Contracts.Appointments;

public sealed record CreateAppointmentRequest
{
    public Guid PatientProfileId { get; init; }
    public Guid DoctorProfileId { get; init; }
    public Guid MedicalServiceId { get; init; }
    public Guid ScheduleSlotId { get; init; }
    public string? Reason { get; init; }
}
