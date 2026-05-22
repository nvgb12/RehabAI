using System.Net.Mail;
using RehabAI.Application.Auth;
using RehabAI.Application.Emails;
using RehabAI.Domain.Enums;

namespace RehabAI.Application.Doctors;

public sealed class DoctorService(
    IDoctorAccountRepository doctorAccountRepository,
    ISecureTokenService tokenService,
    IEmailSender emailSender) : IDoctorService
{
    private const string DoctorRoleName = "Doctor";
    private const int DoctorInvitationTokenLifetimeHours = 72;
    private const string InvitationEmailSubject = "Set up your Rehab AI doctor account";
    private const string InvitationEmailTemplate = "DoctorInvitationPasswordSetup";

    public async Task<CreateDoctorResult> CreateDoctorAsync(
        CreateDoctorCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationMessage = ValidateCreateDoctorCommand(command);

        if (validationMessage is not null)
        {
            return new CreateDoctorResult(false, validationMessage, FailureReason: CreateDoctorFailureReason.Validation);
        }

        var email = NormalizeEmail(command.Email);

        if (await doctorAccountRepository.EmailExistsAsync(email, cancellationToken))
        {
            return new CreateDoctorResult(
                false,
                "An account with this email already exists.",
                Email: email,
                FailureReason: CreateDoctorFailureReason.DuplicateEmail);
        }

        var doctorRoleId = await doctorAccountRepository.GetRoleIdByNameAsync(DoctorRoleName, cancellationToken);

        if (doctorRoleId is null)
        {
            return new CreateDoctorResult(
                false,
                "Doctor role seed data is missing.",
                Email: email,
                FailureReason: CreateDoctorFailureReason.MissingDoctorRole);
        }

        if (!await doctorAccountRepository.SpecialtyExistsAsync(command.SpecialtyId, cancellationToken))
        {
            return new CreateDoctorResult(
                false,
                "Selected specialty was not found.",
                Email: email,
                FailureReason: CreateDoctorFailureReason.SpecialtyNotFound);
        }

        var invitationToken = tokenService.GenerateToken();
        var invitationTokenHash = tokenService.HashToken(invitationToken);
        var invitationExpiresAt = DateTimeOffset.UtcNow.AddHours(DoctorInvitationTokenLifetimeHours);
        var commissionRate = await doctorAccountRepository.GetDefaultCommissionRateAsync(cancellationToken);
        var setupUrl = BuildPasswordSetupUrl(email, invitationToken);

        var account = new CreatedDoctorAccount(
            command.FullName.Trim(),
            email,
            NormalizeOptional(command.PhoneNumber),
            doctorRoleId.Value,
            command.SpecialtyId,
            NormalizeOptional(command.Bio),
            commissionRate,
            invitationTokenHash,
            invitationExpiresAt,
            InvitationEmailSubject,
            InvitationEmailTemplate);

        var created = await doctorAccountRepository.CreateDoctorAccountAsync(account, cancellationToken);

        try
        {
            var emailBody = BuildInvitationEmailBody(command.FullName.Trim(), setupUrl, invitationToken, invitationExpiresAt);
            await emailSender.SendAsync(email, InvitationEmailSubject, emailBody, cancellationToken);
            await doctorAccountRepository.MarkInvitationEmailSentAsync(created.EmailLogId, cancellationToken);
        }
        catch (Exception exception)
        {
            await doctorAccountRepository.MarkInvitationEmailFailedAsync(created.EmailLogId, exception.Message, cancellationToken);

            return new CreateDoctorResult(
                false,
                "Doctor account was created, but the invitation email could not be sent.",
                created.UserId,
                created.DoctorProfileId,
                email,
                invitationToken,
                setupUrl,
                CreateDoctorFailureReason.EmailDeliveryFailed);
        }

        return new CreateDoctorResult(
            true,
            "Doctor account created. Invitation password setup is required.",
            created.UserId,
            created.DoctorProfileId,
            email,
            invitationToken,
            setupUrl);
    }

    public async Task<IReadOnlyList<AdminDoctorResponse>> GetAdminDoctorsAsync(
        CancellationToken cancellationToken = default)
    {
        var doctors = await doctorAccountRepository.GetAdminDoctorsAsync(cancellationToken);

        return doctors
            .Select(ToAdminDoctorResponse)
            .ToList();
    }

    public async Task<AdminDoctorResponse?> GetAdminDoctorByIdAsync(
        Guid doctorProfileId,
        CancellationToken cancellationToken = default)
    {
        if (doctorProfileId == Guid.Empty)
        {
            return null;
        }

        var doctor = await doctorAccountRepository.GetAdminDoctorByIdAsync(doctorProfileId, cancellationToken);

        return doctor is null ? null : ToAdminDoctorResponse(doctor);
    }

    public async Task<AdminDoctorPublicProfileReviewResult> ApprovePublicProfileAsync(
        Guid doctorProfileId,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        if (doctorProfileId == Guid.Empty || adminUserId == Guid.Empty)
        {
            return new AdminDoctorPublicProfileReviewResult(
                false,
                "Doctor profile and Admin user are required.",
                FailureReason: AdminDoctorPublicProfileReviewFailureReason.Validation);
        }

        var existing = await doctorAccountRepository.GetAdminDoctorByIdAsync(doctorProfileId, cancellationToken);
        if (existing is null)
        {
            return new AdminDoctorPublicProfileReviewResult(
                false,
                "Doctor profile was not found.",
                FailureReason: AdminDoctorPublicProfileReviewFailureReason.DoctorNotFound);
        }

        if (existing.PublicProfileReviewStatus != DoctorProfileReviewStatus.Submitted)
        {
            return new AdminDoctorPublicProfileReviewResult(
                false,
                "Only submitted Doctor public profiles can be approved.",
                ToAdminDoctorResponse(existing),
                AdminDoctorPublicProfileReviewFailureReason.InvalidStatus);
        }

        var updated = await doctorAccountRepository.ApprovePublicProfileAsync(
            doctorProfileId,
            adminUserId,
            DateTimeOffset.UtcNow,
            cancellationToken);

        return updated is null
            ? new AdminDoctorPublicProfileReviewResult(
                false,
                "Doctor profile was not found.",
                FailureReason: AdminDoctorPublicProfileReviewFailureReason.DoctorNotFound)
            : new AdminDoctorPublicProfileReviewResult(
                true,
                "Doctor public profile approved.",
                ToAdminDoctorResponse(updated));
    }

    public async Task<AdminDoctorPublicProfileReviewResult> RejectPublicProfileAsync(
        Guid doctorProfileId,
        Guid adminUserId,
        string rejectionReason,
        CancellationToken cancellationToken = default)
    {
        if (doctorProfileId == Guid.Empty || adminUserId == Guid.Empty)
        {
            return new AdminDoctorPublicProfileReviewResult(
                false,
                "Doctor profile and Admin user are required.",
                FailureReason: AdminDoctorPublicProfileReviewFailureReason.Validation);
        }

        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            return new AdminDoctorPublicProfileReviewResult(
                false,
                "Rejection reason is required.",
                FailureReason: AdminDoctorPublicProfileReviewFailureReason.Validation);
        }

        var existing = await doctorAccountRepository.GetAdminDoctorByIdAsync(doctorProfileId, cancellationToken);
        if (existing is null)
        {
            return new AdminDoctorPublicProfileReviewResult(
                false,
                "Doctor profile was not found.",
                FailureReason: AdminDoctorPublicProfileReviewFailureReason.DoctorNotFound);
        }

        if (existing.PublicProfileReviewStatus != DoctorProfileReviewStatus.Submitted)
        {
            return new AdminDoctorPublicProfileReviewResult(
                false,
                "Only submitted Doctor public profiles can be rejected.",
                ToAdminDoctorResponse(existing),
                AdminDoctorPublicProfileReviewFailureReason.InvalidStatus);
        }

        var updated = await doctorAccountRepository.RejectPublicProfileAsync(
            doctorProfileId,
            adminUserId,
            NormalizeOptional(rejectionReason)!,
            DateTimeOffset.UtcNow,
            cancellationToken);

        return updated is null
            ? new AdminDoctorPublicProfileReviewResult(
                false,
                "Doctor profile was not found.",
                FailureReason: AdminDoctorPublicProfileReviewFailureReason.DoctorNotFound)
            : new AdminDoctorPublicProfileReviewResult(
                true,
                "Doctor public profile rejected.",
                ToAdminDoctorResponse(updated));
    }

    public Task ResendInvitationAsync(Guid doctorProfileId, Guid adminUserId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Resend invitation is not part of the current Admin-created Doctor account MVP task.");
    }

    private static AdminDoctorResponse ToAdminDoctorResponse(AdminDoctorRecord doctor)
    {
        var missingItems = GetPublicProfileMissingItems(doctor);

        return new AdminDoctorResponse(
            doctor.DoctorProfileId,
            doctor.UserId,
            doctor.FullName,
            doctor.Email,
            doctor.PhoneNumber,
            doctor.Status.ToString(),
            doctor.EmailConfirmed,
            doctor.SpecialtyId,
            doctor.SpecialtyName,
            doctor.Bio,
            doctor.AvatarUrl,
            doctor.PublicProfileApproved,
            doctor.PublicProfileReviewStatus.ToString(),
            doctor.SubmittedForReviewAt,
            doctor.ReviewedAt,
            doctor.ReviewedByAdminId,
            doctor.PublicProfileRejectionReason,
            missingItems.Count == 0,
            missingItems,
            doctor.CreatedAt,
            doctor.UpdatedAt,
            doctor.IsDeleted);
    }

    private static IReadOnlyList<string> GetPublicProfileMissingItems(AdminDoctorRecord doctor)
    {
        var missingItems = new List<string>();

        if (doctor.Status != AccountStatus.Active)
        {
            missingItems.Add("Active account status");
        }

        if (!doctor.EmailConfirmed)
        {
            missingItems.Add("Confirmed email");
        }

        if (doctor.SpecialtyId == Guid.Empty)
        {
            missingItems.Add("Specialty");
        }

        if (string.IsNullOrWhiteSpace(doctor.PhoneNumber))
        {
            missingItems.Add("Phone number");
        }

        if (string.IsNullOrWhiteSpace(doctor.Bio))
        {
            missingItems.Add("Bio");
        }

        if (string.IsNullOrWhiteSpace(doctor.AvatarUrl))
        {
            missingItems.Add("Avatar/profile image");
        }

        return missingItems;
    }

    private static string? ValidateCreateDoctorCommand(CreateDoctorCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.FullName))
        {
            return "Full name is required.";
        }

        if (string.IsNullOrWhiteSpace(command.Email) || !IsValidEmail(command.Email))
        {
            return "A valid email is required.";
        }

        if (command.SpecialtyId == Guid.Empty)
        {
            return "Specialty is required.";
        }

        return null;
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

    private static string BuildPasswordSetupUrl(string email, string token)
    {
        var escapedEmail = Uri.EscapeDataString(email);
        var escapedToken = Uri.EscapeDataString(token);

        return $"/doctor/setup-password?email={escapedEmail}&token={escapedToken}";
    }

    private static string BuildInvitationEmailBody(
        string fullName,
        string setupUrl,
        string token,
        DateTimeOffset expiresAt)
    {
        return $"""
            <p>Hello {fullName},</p>
            <p>An administrator created your Rehab AI doctor account.</p>
            <p>Please set your initial password using this setup URL:</p>
            <p><strong>{setupUrl}</strong></p>
            <p>Development token: <strong>{token}</strong></p>
            <p>This invitation expires at {expiresAt:O}.</p>
            """;
    }
}
