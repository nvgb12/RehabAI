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
