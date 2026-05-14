namespace RehabAI.Application.Appointments;

public sealed record CreateAppointmentCommand(Guid PatientId, Guid DoctorProfileId, Guid MedicalServiceId, DateTimeOffset StartTime, string? Notes);

public interface IAppointmentService
{
    Task<Guid> CreateAsync(CreateAppointmentCommand command, CancellationToken cancellationToken = default);
    Task ConfirmAsync(Guid appointmentId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid appointmentId, Guid actorUserId, string reason, CancellationToken cancellationToken = default);
}
