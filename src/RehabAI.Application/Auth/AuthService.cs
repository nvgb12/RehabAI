using System.Net.Mail;
using RehabAI.Application.Emails;
using RehabAI.Domain.Enums;

namespace RehabAI.Application.Auth;

public sealed class AuthService(
    IPatientRegistrationRepository registrationRepository,
    IUserAuthenticationRepository authenticationRepository,
    IPasswordHasher passwordHasher,
    ISecureTokenService tokenService,
    IJwtTokenService jwtTokenService,
    IEmailSender emailSender) : IAuthService
{
    private const string PatientRoleName = "Patient";
    private const int EmailVerificationTokenLifetimeHours = 24;
    private const string VerificationEmailSubject = "Verify your Rehab AI account";
    private const string VerificationEmailTemplate = "PatientEmailVerification";

    public async Task<RegisterPatientResult> RegisterPatientAsync(
        RegisterPatientCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationMessage = ValidateRegisterPatientCommand(command);

        if (validationMessage is not null)
        {
            return new RegisterPatientResult(false, validationMessage, FailureReason: RegisterPatientFailureReason.Validation);
        }

        var email = NormalizeEmail(command.Email);

        if (await registrationRepository.EmailExistsAsync(email, cancellationToken))
        {
            return new RegisterPatientResult(
                false,
                "An account with this email already exists.",
                Email: email,
                FailureReason: RegisterPatientFailureReason.DuplicateEmail);
        }

        var patientRoleId = await registrationRepository.GetRoleIdByNameAsync(PatientRoleName, cancellationToken);

        if (patientRoleId is null)
        {
            return new RegisterPatientResult(
                false,
                "Patient role seed data is missing.",
                Email: email,
                FailureReason: RegisterPatientFailureReason.MissingPatientRole);
        }

        var verificationToken = tokenService.GenerateToken();
        var tokenHash = tokenService.HashToken(verificationToken);
        var passwordHash = passwordHasher.HashPassword(command.Password);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(EmailVerificationTokenLifetimeHours);

        var registration = new PendingPatientRegistration(
            command.FullName.Trim(),
            email,
            NormalizeOptional(command.PhoneNumber),
            passwordHash,
            patientRoleId.Value,
            tokenHash,
            expiresAt,
            VerificationEmailSubject,
            VerificationEmailTemplate);

        var created = await registrationRepository.CreatePendingPatientAsync(registration, cancellationToken);

        try
        {
            var emailBody = BuildVerificationEmailBody(command.FullName.Trim(), verificationToken, expiresAt);
            await emailSender.SendAsync(email, VerificationEmailSubject, emailBody, cancellationToken);
            await registrationRepository.MarkVerificationEmailSentAsync(created.EmailLogId, cancellationToken);
        }
        catch (Exception exception)
        {
            await registrationRepository.MarkVerificationEmailFailedAsync(created.EmailLogId, exception.Message, cancellationToken);

            return new RegisterPatientResult(
                false,
                "Patient account was created, but the verification email could not be sent.",
                created.UserId,
                email,
                RegisterPatientFailureReason.EmailDeliveryFailed);
        }

        return new RegisterPatientResult(
            true,
            "Patient account created. Please verify your email before logging in.",
            created.UserId,
            email,
            VerificationToken: verificationToken);
    }

    public async Task<VerifyEmailResult> VerifyEmailAsync(
        VerifyEmailCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationMessage = ValidateVerifyEmailCommand(command);

        if (validationMessage is not null)
        {
            return new VerifyEmailResult(false, validationMessage, FailureReason: VerifyEmailFailureReason.Validation);
        }

        var email = NormalizeEmail(command.Email);
        var tokenHash = tokenService.HashToken(command.Token.Trim());
        var tokenRecords = await registrationRepository.GetEmailVerificationTokensAsync(email, cancellationToken);
        var tokenRecord = tokenRecords.SingleOrDefault(record => tokenService.TokenHashesEqual(tokenHash, record.TokenHash));

        if (tokenRecord is null)
        {
            return new VerifyEmailResult(
                false,
                "Email verification token is invalid.",
                Email: email,
                FailureReason: VerifyEmailFailureReason.InvalidToken);
        }

        if (tokenRecord.UsedAt is not null)
        {
            return new VerifyEmailResult(
                false,
                "Email verification token has already been used.",
                tokenRecord.UserId,
                email,
                VerifyEmailFailureReason.UsedToken);
        }

        if (tokenRecord.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return new VerifyEmailResult(
                false,
                "Email verification token has expired.",
                tokenRecord.UserId,
                email,
                VerifyEmailFailureReason.ExpiredToken);
        }

        await registrationRepository.CompleteEmailVerificationAsync(tokenRecord.UserId, tokenRecord.TokenId, cancellationToken);

        return new VerifyEmailResult(
            true,
            "Email verified successfully. Patient account is now active.",
            tokenRecord.UserId,
            email);
    }

    public async Task<LoginResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var validationMessage = ValidateLoginCommand(command);

        if (validationMessage is not null)
        {
            return new LoginResult(false, validationMessage, FailureReason: LoginFailureReason.Validation);
        }

        var email = NormalizeEmail(command.Email);
        var user = await authenticationRepository.GetUserForLoginAsync(email, cancellationToken);

        if (user is null)
        {
            return new LoginResult(
                false,
                "Email or password is incorrect.",
                Email: email,
                FailureReason: LoginFailureReason.InvalidCredentials);
        }

        var accountStatus = (AccountStatus)user.Status;
        var blockedMessage = GetBlockedLoginMessage(accountStatus);

        if (blockedMessage is not null)
        {
            return new LoginResult(
                false,
                blockedMessage,
                user.UserId,
                user.Email,
                user.FullName,
                user.Roles,
                FailureReason: LoginFailureReason.AccountBlocked);
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash) ||
            !passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
        {
            return new LoginResult(
                false,
                "Email or password is incorrect.",
                Email: email,
                FailureReason: LoginFailureReason.InvalidCredentials);
        }

        var accessToken = jwtTokenService.CreateAccessToken(user);

        return new LoginResult(
            true,
            "Login successful.",
            user.UserId,
            user.Email,
            user.FullName,
            user.Roles,
            accessToken);
    }

    public async Task<SetupDoctorPasswordResult> SetupDoctorPasswordAsync(
        SetupDoctorPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationMessage = ValidateSetupDoctorPasswordCommand(command);

        if (validationMessage is not null)
        {
            return new SetupDoctorPasswordResult(false, validationMessage, FailureReason: SetupDoctorPasswordFailureReason.Validation);
        }

        var email = NormalizeEmail(command.Email);
        var tokenHash = tokenService.HashToken(command.Token.Trim());
        var tokenRecords = await registrationRepository.GetDoctorInvitationTokensAsync(email, cancellationToken);
        var tokenRecord = tokenRecords.SingleOrDefault(record => tokenService.TokenHashesEqual(tokenHash, record.TokenHash));

        if (tokenRecord is null)
        {
            return new SetupDoctorPasswordResult(
                false,
                "Doctor invitation token is invalid.",
                Email: email,
                FailureReason: SetupDoctorPasswordFailureReason.InvalidToken);
        }

        if (tokenRecord.UsedAt is not null)
        {
            return new SetupDoctorPasswordResult(
                false,
                "Doctor invitation token has already been used.",
                tokenRecord.UserId,
                email,
                SetupDoctorPasswordFailureReason.UsedToken);
        }

        if (tokenRecord.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return new SetupDoctorPasswordResult(
                false,
                "Doctor invitation token has expired.",
                tokenRecord.UserId,
                email,
                SetupDoctorPasswordFailureReason.ExpiredToken);
        }

        var passwordHash = passwordHasher.HashPassword(command.Password);
        await registrationRepository.CompleteDoctorPasswordSetupAsync(
            tokenRecord.UserId,
            tokenRecord.TokenId,
            passwordHash,
            cancellationToken);

        return new SetupDoctorPasswordResult(
            true,
            "Password setup completed. Doctor account is now active.",
            tokenRecord.UserId,
            email);
    }

    public Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Password reset is not part of the current Patient registration MVP task.");
    }

    public Task ResetPasswordAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Password reset is not part of the current Patient registration MVP task.");
    }

    private static string? ValidateRegisterPatientCommand(RegisterPatientCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.FullName))
        {
            return "Full name is required.";
        }

        if (string.IsNullOrWhiteSpace(command.Email) || !IsValidEmail(command.Email))
        {
            return "A valid email is required.";
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            return "Password is required.";
        }

        return null;
    }

    private static string? ValidateVerifyEmailCommand(VerifyEmailCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Email) || !IsValidEmail(command.Email))
        {
            return "A valid email is required.";
        }

        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return "Verification token is required.";
        }

        return null;
    }

    private static string? ValidateLoginCommand(LoginCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Email) || !IsValidEmail(command.Email))
        {
            return "A valid email is required.";
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            return "Password is required.";
        }

        return null;
    }

    private static string? ValidateSetupDoctorPasswordCommand(SetupDoctorPasswordCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Email) || !IsValidEmail(command.Email))
        {
            return "A valid email is required.";
        }

        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return "Doctor invitation token is required.";
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            return "Password is required.";
        }

        return null;
    }

    private static string? GetBlockedLoginMessage(AccountStatus status)
    {
        return status switch
        {
            AccountStatus.Active => null,
            AccountStatus.PendingEmail => "Please verify your email before logging in.",
            AccountStatus.PendingPasswordSetup => "Please complete the doctor invitation password setup flow before logging in.",
            AccountStatus.Locked => "This account is locked.",
            AccountStatus.Suspended => "This account is suspended.",
            AccountStatus.Deactivated => "This account is deactivated.",
            _ => "This account cannot log in."
        };
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email.Trim());
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string BuildVerificationEmailBody(string fullName, string token, DateTimeOffset expiresAt)
    {
        return $"""
            <p>Hello {fullName},</p>
            <p>Please verify your Rehab AI account using this verification token:</p>
            <p><strong>{token}</strong></p>
            <p>This token expires at {expiresAt:O}.</p>
            """;
    }
}
