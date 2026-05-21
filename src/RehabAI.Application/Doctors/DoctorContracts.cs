using RehabAI.Domain.Enums;

namespace RehabAI.Application.Doctors;

public sealed record CreateDoctorCommand(
    string FullName,
    string Email,
    string? PhoneNumber,
    Guid SpecialtyId,
    string? Bio,
    int? YearsOfExperience);

public sealed record CreateDoctorResult(
    bool Succeeded,
    string Message,
    Guid? UserId = null,
    Guid? DoctorProfileId = null,
    string? Email = null,
    string? InvitationToken = null,
    string? PasswordSetupUrl = null,
    CreateDoctorFailureReason? FailureReason = null);

public enum CreateDoctorFailureReason
{
    Validation = 1,
    DuplicateEmail = 2,
    MissingDoctorRole = 3,
    SpecialtyNotFound = 4,
    EmailDeliveryFailed = 5
}

public interface IDoctorService
{
    Task<CreateDoctorResult> CreateDoctorAsync(CreateDoctorCommand command, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminDoctorResponse>> GetAdminDoctorsAsync(CancellationToken cancellationToken = default);
    Task ResendInvitationAsync(Guid doctorProfileId, Guid adminUserId, CancellationToken cancellationToken = default);
}

public interface IDoctorAccountRepository
{
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<Guid?> GetRoleIdByNameAsync(string roleName, CancellationToken cancellationToken = default);
    Task<bool> SpecialtyExistsAsync(Guid specialtyId, CancellationToken cancellationToken = default);
    Task<decimal> GetDefaultCommissionRateAsync(CancellationToken cancellationToken = default);
    Task<CreatedDoctorAccountResult> CreateDoctorAccountAsync(
        CreatedDoctorAccount account,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminDoctorRecord>> GetAdminDoctorsAsync(CancellationToken cancellationToken = default);
    Task MarkInvitationEmailSentAsync(Guid emailLogId, CancellationToken cancellationToken = default);
    Task MarkInvitationEmailFailedAsync(Guid emailLogId, string errorMessage, CancellationToken cancellationToken = default);
}

public sealed record CreatedDoctorAccount(
    string FullName,
    string Email,
    string? PhoneNumber,
    Guid DoctorRoleId,
    Guid SpecialtyId,
    string? Bio,
    decimal CommissionRate,
    string InvitationTokenHash,
    DateTimeOffset InvitationTokenExpiresAt,
    string EmailSubject,
    string EmailTemplateName);

public sealed record CreatedDoctorAccountResult(Guid UserId, Guid DoctorProfileId, Guid EmailLogId);

public sealed record AdminDoctorRecord(
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
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    bool IsDeleted);

public sealed record AdminDoctorResponse(
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
    bool PublicProfileApproved,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    bool IsDeleted);
