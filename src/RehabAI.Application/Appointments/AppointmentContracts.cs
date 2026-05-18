using RehabAI.Domain.Enums;

namespace RehabAI.Application.Appointments;

public sealed record CreateAppointmentCommand(
    Guid PatientProfileId,
    Guid DoctorProfileId,
    Guid MedicalServiceId,
    Guid ScheduleSlotId,
    string? Reason);

public sealed record AppointmentResponse(
    Guid Id,
    Guid PatientProfileId,
    Guid DoctorProfileId,
    Guid MedicalServiceId,
    Guid ScheduleSlotId,
    string Status,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    DateTimeOffset? ReservedUntil,
    string? Reason);

public sealed record AppointmentResult(
    bool Succeeded,
    string Message,
    AppointmentResponse? Appointment = null,
    AppointmentFailureReason? FailureReason = null);

public enum AppointmentFailureReason
{
    Validation = 1,
    PatientNotFound = 2,
    PatientNotActive = 3,
    PatientRoleMissing = 4,
    DoctorNotFound = 5,
    DoctorNotPublicBookable = 6,
    MedicalServiceNotFound = 7,
    SlotNotFound = 8,
    SlotUnavailable = 9,
    DoubleBooked = 10
}

public interface IAppointmentBookingService
{
    Task<AppointmentResult> CreateAsync(CreateAppointmentCommand command, CancellationToken cancellationToken = default);
    Task<AppointmentResponse?> GetByIdAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppointmentResponse>> GetPatientAppointmentsAsync(Guid patientProfileId, CancellationToken cancellationToken = default);
}

public interface IAppointmentBookingRepository
{
    Task<PatientBookingState?> GetPatientStateAsync(Guid patientProfileId, CancellationToken cancellationToken = default);
    Task<DoctorBookingState?> GetDoctorStateAsync(Guid doctorProfileId, CancellationToken cancellationToken = default);
    Task<bool> MedicalServiceIsActiveAsync(Guid medicalServiceId, CancellationToken cancellationToken = default);
    Task<int?> GetSoftReserveMinutesAsync(CancellationToken cancellationToken = default);
    Task<CreateAppointmentRepositoryResult> CreatePendingPaymentAppointmentAsync(
        CreateAppointmentDraft draft,
        CancellationToken cancellationToken = default);
    Task<AppointmentRecord?> GetByIdAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppointmentRecord>> GetByPatientProfileIdAsync(Guid patientProfileId, CancellationToken cancellationToken = default);
}

public sealed record PatientBookingState(
    Guid PatientProfileId,
    Guid UserId,
    bool IsActive,
    bool HasPatientRole);

public sealed record DoctorBookingState(
    Guid DoctorProfileId,
    Guid UserId,
    bool IsPublicBookable);

public sealed record CreateAppointmentDraft(
    Guid PatientProfileId,
    Guid PatientUserId,
    Guid DoctorProfileId,
    Guid MedicalServiceId,
    Guid ScheduleSlotId,
    string? Reason,
    DateTimeOffset ReservedUntil);

public sealed record CreateAppointmentRepositoryResult(
    bool Succeeded,
    AppointmentRecord? Appointment,
    AppointmentFailureReason? FailureReason,
    string? Message);

public sealed record AppointmentRecord(
    Guid Id,
    Guid PatientProfileId,
    Guid PatientUserId,
    Guid DoctorProfileId,
    Guid MedicalServiceId,
    Guid ScheduleSlotId,
    AppointmentStatus Status,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    DateTimeOffset? ReservedUntil,
    string? Reason);
