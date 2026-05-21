namespace RehabAI.Application.Auth;

public sealed record RegisterPatientCommand(string FullName, string Email, string? PhoneNumber, string Password);
public sealed record VerifyEmailCommand(string Email, string Token);
public sealed record LoginCommand(string Email, string Password);
public sealed record SetupDoctorPasswordCommand(string Email, string Token, string Password);
public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword);

public sealed record RegisterPatientResult(
    bool Succeeded,
    string Message,
    Guid? UserId = null,
    string? Email = null,
    RegisterPatientFailureReason? FailureReason = null,
    string? VerificationToken = null);

public enum RegisterPatientFailureReason
{
    Validation = 1,
    DuplicateEmail = 2,
    MissingPatientRole = 3,
    EmailDeliveryFailed = 4
}

public sealed record VerifyEmailResult(
    bool Succeeded,
    string Message,
    Guid? UserId = null,
    string? Email = null,
    VerifyEmailFailureReason? FailureReason = null);

public enum VerifyEmailFailureReason
{
    Validation = 1,
    InvalidToken = 2,
    ExpiredToken = 3,
    UsedToken = 4
}

public sealed record LoginResult(
    bool Succeeded,
    string Message,
    Guid? UserId = null,
    string? Email = null,
    string? FullName = null,
    IReadOnlyList<string>? Roles = null,
    string? AccessToken = null,
    Guid? PatientProfileId = null,
    LoginFailureReason? FailureReason = null);

public enum LoginFailureReason
{
    Validation = 1,
    InvalidCredentials = 2,
    AccountBlocked = 3
}

public sealed record SetupDoctorPasswordResult(
    bool Succeeded,
    string Message,
    Guid? UserId = null,
    string? Email = null,
    SetupDoctorPasswordFailureReason? FailureReason = null);

public enum SetupDoctorPasswordFailureReason
{
    Validation = 1,
    InvalidToken = 2,
    ExpiredToken = 3,
    UsedToken = 4
}

public interface IAuthService
{
    Task<RegisterPatientResult> RegisterPatientAsync(RegisterPatientCommand command, CancellationToken cancellationToken = default);
    Task<VerifyEmailResult> VerifyEmailAsync(VerifyEmailCommand command, CancellationToken cancellationToken = default);
    Task<LoginResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default);
    Task<SetupDoctorPasswordResult> SetupDoctorPasswordAsync(SetupDoctorPasswordCommand command, CancellationToken cancellationToken = default);
    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default);
}

public interface IUserAuthenticationRepository
{
    Task<UserAuthenticationRecord?> GetUserForLoginAsync(string normalizedEmail, CancellationToken cancellationToken = default);
}

public interface IPatientRegistrationRepository
{
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<Guid?> GetRoleIdByNameAsync(string roleName, CancellationToken cancellationToken = default);
    Task<PendingPatientRegistrationResult> CreatePendingPatientAsync(PendingPatientRegistration registration, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailVerificationTokenRecord>> GetEmailVerificationTokensAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task CompleteEmailVerificationAsync(Guid userId, Guid tokenId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoctorInvitationTokenRecord>> GetDoctorInvitationTokensAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task CompleteDoctorPasswordSetupAsync(Guid userId, Guid tokenId, string passwordHash, CancellationToken cancellationToken = default);
    Task MarkVerificationEmailSentAsync(Guid emailLogId, CancellationToken cancellationToken = default);
    Task MarkVerificationEmailFailedAsync(Guid emailLogId, string errorMessage, CancellationToken cancellationToken = default);
}

public sealed record PendingPatientRegistration(
    string FullName,
    string Email,
    string? PhoneNumber,
    string PasswordHash,
    Guid PatientRoleId,
    string VerificationTokenHash,
    DateTimeOffset VerificationTokenExpiresAt,
    string EmailSubject,
    string EmailTemplateName);

public sealed record PendingPatientRegistrationResult(Guid UserId, Guid EmailLogId);

public sealed record EmailVerificationTokenRecord(
    Guid UserId,
    Guid TokenId,
    string Email,
    string TokenHash,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? UsedAt);

public sealed record DoctorInvitationTokenRecord(
    Guid UserId,
    Guid TokenId,
    string Email,
    string TokenHash,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? UsedAt);

public sealed record UserAuthenticationRecord(
    Guid UserId,
    string Email,
    string FullName,
    string? PasswordHash,
    int Status,
    IReadOnlyList<string> Roles,
    Guid? PatientProfileId = null);

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}

public interface ISecureTokenService
{
    string GenerateToken();
    string HashToken(string token);
    bool TokenHashesEqual(string hash, string expectedHash);
}

public interface IJwtTokenService
{
    string CreateAccessToken(UserAuthenticationRecord user);
}
