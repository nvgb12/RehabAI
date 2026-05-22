using RehabAI.Domain.Enums;

namespace RehabAI.Application.Doctors;

public sealed record DoctorProfileResponse(
    Guid DoctorProfileId,
    Guid UserId,
    string FullName,
    string Email,
    string? PhoneNumber,
    string Status,
    bool EmailConfirmed,
    Guid SpecialtyId,
    string SpecialtyName,
    string? Bio,
    int? YearsOfExperience,
    bool PublicProfileApproved,
    string PublicProfileReviewStatus,
    DateTimeOffset? SubmittedForReviewAt,
    DateTimeOffset? ReviewedAt,
    Guid? ReviewedByAdminId,
    string? PublicProfileRejectionReason,
    string? AvatarUrl,
    string? ProfileImageUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record UpdateDoctorProfileCommand(
    string? PhoneNumber,
    string? Bio);

public sealed record DoctorProfileResult(
    bool Succeeded,
    string Message,
    DoctorProfileResponse? Profile = null,
    DoctorDashboardFailureReason? FailureReason = null);

public sealed record DoctorAppointmentResponse(
    Guid AppointmentId,
    Guid PatientProfileId,
    string PatientName,
    Guid MedicalServiceId,
    string MedicalServiceName,
    Guid? DoctorScheduleSlotId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string Status,
    string? PaymentStatus,
    string? Notes,
    DateTimeOffset CreatedAt);

public sealed record DoctorDashboardSummaryResponse(
    Guid DoctorProfileId,
    string FullName,
    bool PublicProfileApproved,
    int UpcomingAppointmentCount,
    int TodayAppointmentCount,
    int AvailableSlotCount,
    int BookedSlotCount,
    DoctorDashboardNextAppointmentResponse? NextAppointment);

public sealed record DoctorDashboardNextAppointmentResponse(
    Guid AppointmentId,
    string PatientName,
    string MedicalServiceName,
    DateTimeOffset StartTime,
    string Status);

public sealed record UploadDoctorAvatarCommand(
    Guid UserId,
    string FileName,
    string ContentType,
    long Length,
    Stream Content);

public sealed record DoctorAvatarUploadResult(
    bool Succeeded,
    string Message,
    string? AvatarUrl = null,
    DoctorDashboardFailureReason? FailureReason = null);

public sealed record DoctorPublicProfileSubmitResult(
    bool Succeeded,
    string Message,
    DoctorProfileResponse? Profile = null,
    IReadOnlyList<string>? MissingReadinessItems = null,
    DoctorDashboardFailureReason? FailureReason = null);

public sealed record DoctorAppointmentActionResult(
    bool Succeeded,
    string Message,
    DoctorAppointmentResponse? Appointment = null,
    DoctorDashboardFailureReason? FailureReason = null);

public enum DoctorDashboardFailureReason
{
    Validation = 1,
    DoctorNotFound = 2,
    AppointmentNotFound = 3,
    FileTooLarge = 4,
    InvalidStatus = 5
}

public interface IDoctorDashboardService
{
    Task<DoctorProfileResponse?> GetOwnProfileAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<DoctorProfileResult> UpdateOwnProfileAsync(
        Guid currentUserId,
        UpdateDoctorProfileCommand command,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoctorAppointmentResponse>> GetOwnAppointmentsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default);
    Task<DoctorAppointmentResponse?> GetOwnAppointmentByIdAsync(
        Guid currentUserId,
        Guid appointmentId,
        CancellationToken cancellationToken = default);
    Task<DoctorDashboardSummaryResponse?> GetDashboardAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default);
    Task<DoctorAvatarUploadResult> UploadAvatarAsync(
        UploadDoctorAvatarCommand command,
        CancellationToken cancellationToken = default);
    Task<DoctorPublicProfileSubmitResult> SubmitPublicProfileForReviewAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoctorAppointmentResponse>> GetOwnAppointmentRequestsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default);
    Task<DoctorAppointmentActionResult> AcceptAppointmentRequestAsync(
        Guid currentUserId,
        Guid appointmentId,
        CancellationToken cancellationToken = default);
    Task<DoctorAppointmentActionResult> RejectAppointmentRequestAsync(
        Guid currentUserId,
        Guid appointmentId,
        string? rejectionReason,
        CancellationToken cancellationToken = default);
}

public interface IDoctorDashboardRepository
{
    Task<DoctorProfileRecord?> GetProfileByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<DoctorProfileRecord?> UpdateOwnProfileAsync(
        Guid userId,
        UpdateDoctorProfileCommand command,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoctorAppointmentRecord>> GetAppointmentsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<DoctorAppointmentRecord?> GetAppointmentByUserIdAsync(
        Guid userId,
        Guid appointmentId,
        CancellationToken cancellationToken = default);
    Task<DoctorDashboardSnapshot?> GetDashboardSnapshotAsync(
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
    Task<string?> UpdateAvatarAsync(
        Guid userId,
        string avatarUrl,
        CancellationToken cancellationToken = default);
    Task<DoctorProfileRecord?> SubmitPublicProfileForReviewAsync(
        Guid userId,
        DateTimeOffset submittedAt,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoctorAppointmentRecord>> GetRequestedAppointmentsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<DoctorAppointmentRecord?> AcceptAppointmentRequestAsync(
        Guid userId,
        Guid appointmentId,
        CancellationToken cancellationToken = default);
    Task<DoctorAppointmentRecord?> RejectAppointmentRequestAsync(
        Guid userId,
        Guid appointmentId,
        string rejectionReason,
        CancellationToken cancellationToken = default);
}

public interface IDoctorAvatarStorage
{
    Task<string> SaveAsync(
        Stream content,
        string fileExtension,
        CancellationToken cancellationToken = default);
}

public sealed record DoctorProfileRecord(
    Guid DoctorProfileId,
    Guid UserId,
    string FullName,
    string Email,
    string? PhoneNumber,
    AccountStatus Status,
    bool EmailConfirmed,
    Guid SpecialtyId,
    string SpecialtyName,
    string? Bio,
    bool PublicProfileApproved,
    DoctorProfileReviewStatus PublicProfileReviewStatus,
    DateTimeOffset? SubmittedForReviewAt,
    DateTimeOffset? ReviewedAt,
    Guid? ReviewedByAdminId,
    string? PublicProfileRejectionReason,
    string? AvatarUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record DoctorAppointmentRecord(
    Guid AppointmentId,
    Guid PatientProfileId,
    string PatientName,
    Guid MedicalServiceId,
    string MedicalServiceName,
    Guid? DoctorScheduleSlotId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    AppointmentStatus Status,
    PaymentStatus? PaymentStatus,
    string? Notes,
    DateTimeOffset CreatedAt);

public sealed record DoctorDashboardSnapshot(
    DoctorProfileRecord Profile,
    int UpcomingAppointmentCount,
    int TodayAppointmentCount,
    int AvailableSlotCount,
    int BookedSlotCount,
    DoctorAppointmentRecord? NextAppointment);
