using System.IO;
using RehabAI.Domain.Enums;

namespace RehabAI.Application.Doctors;

public sealed class DoctorDashboardService(
    IDoctorDashboardRepository repository,
    IDoctorAvatarStorage avatarStorage) : IDoctorDashboardService
{
    private const long MaxAvatarBytes = 2 * 1024 * 1024;
    private static readonly Dictionary<string, string> AllowedContentTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };

    public async Task<DoctorProfileResponse?> GetOwnProfileAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (currentUserId == Guid.Empty)
        {
            return null;
        }

        var profile = await repository.GetProfileByUserIdAsync(currentUserId, cancellationToken);

        return profile is null ? null : ToProfileResponse(profile);
    }

    public async Task<DoctorProfileResult> UpdateOwnProfileAsync(
        Guid currentUserId,
        UpdateDoctorProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        if (currentUserId == Guid.Empty)
        {
            return ProfileFailed("Authenticated user is required.", DoctorDashboardFailureReason.Validation);
        }

        var normalizedCommand = new UpdateDoctorProfileCommand(
            NormalizeOptional(command.PhoneNumber),
            NormalizeOptional(command.Bio));

        var updated = await repository.UpdateOwnProfileAsync(currentUserId, normalizedCommand, cancellationToken);

        if (updated is null)
        {
            return ProfileFailed("Doctor profile was not found.", DoctorDashboardFailureReason.DoctorNotFound);
        }

        return new DoctorProfileResult(
            true,
            "Doctor profile updated successfully.",
            ToProfileResponse(updated));
    }

    public async Task<IReadOnlyList<DoctorAppointmentResponse>> GetOwnAppointmentsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (currentUserId == Guid.Empty)
        {
            return [];
        }

        var appointments = await repository.GetAppointmentsByUserIdAsync(currentUserId, cancellationToken);

        return appointments.Select(ToAppointmentResponse).ToList();
    }

    public async Task<DoctorAppointmentResponse?> GetOwnAppointmentByIdAsync(
        Guid currentUserId,
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        if (currentUserId == Guid.Empty || appointmentId == Guid.Empty)
        {
            return null;
        }

        var appointment = await repository.GetAppointmentByUserIdAsync(
            currentUserId,
            appointmentId,
            cancellationToken);

        return appointment is null ? null : ToAppointmentResponse(appointment);
    }

    public async Task<DoctorDashboardSummaryResponse?> GetDashboardAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (currentUserId == Guid.Empty)
        {
            return null;
        }

        var snapshot = await repository.GetDashboardSnapshotAsync(
            currentUserId,
            DateTimeOffset.UtcNow,
            cancellationToken);

        if (snapshot is null)
        {
            return null;
        }

        return new DoctorDashboardSummaryResponse(
            snapshot.Profile.DoctorProfileId,
            snapshot.Profile.FullName,
            snapshot.Profile.PublicProfileApproved,
            snapshot.UpcomingAppointmentCount,
            snapshot.TodayAppointmentCount,
            snapshot.AvailableSlotCount,
            snapshot.BookedSlotCount,
            snapshot.NextAppointment is null
                ? null
                : new DoctorDashboardNextAppointmentResponse(
                    snapshot.NextAppointment.AppointmentId,
                    snapshot.NextAppointment.PatientName,
                    snapshot.NextAppointment.MedicalServiceName,
                    snapshot.NextAppointment.StartTime,
                    snapshot.NextAppointment.Status.ToString()));
    }

    public async Task<DoctorAvatarUploadResult> UploadAvatarAsync(
        UploadDoctorAvatarCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty)
        {
            return AvatarFailed("Authenticated user is required.", DoctorDashboardFailureReason.Validation);
        }

        if (command.Length <= 0)
        {
            return AvatarFailed("Doctor avatar file is required.", DoctorDashboardFailureReason.Validation);
        }

        if (command.Length > MaxAvatarBytes)
        {
            return AvatarFailed("Doctor avatar must be 2MB or smaller.", DoctorDashboardFailureReason.FileTooLarge);
        }

        var extension = Path.GetExtension(command.FileName);
        if (!IsAllowedImage(extension, command.ContentType))
        {
            return AvatarFailed("Doctor avatar must be a JPG, JPEG, PNG, or WEBP file.", DoctorDashboardFailureReason.Validation);
        }

        var profile = await repository.GetProfileByUserIdAsync(command.UserId, cancellationToken);
        if (profile is null)
        {
            return AvatarFailed("Doctor profile was not found.", DoctorDashboardFailureReason.DoctorNotFound);
        }

        var avatarUrl = await avatarStorage.SaveAsync(command.Content, extension, cancellationToken);
        var savedUrl = await repository.UpdateAvatarAsync(command.UserId, avatarUrl, cancellationToken);

        return savedUrl is null
            ? AvatarFailed("Doctor profile was not found.", DoctorDashboardFailureReason.DoctorNotFound)
            : new DoctorAvatarUploadResult(true, "Doctor avatar uploaded successfully.", savedUrl);
    }

    public async Task<DoctorPublicProfileSubmitResult> SubmitPublicProfileForReviewAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (currentUserId == Guid.Empty)
        {
            return SubmitFailed(
                "Authenticated user is required.",
                DoctorDashboardFailureReason.Validation);
        }

        var profile = await repository.GetProfileByUserIdAsync(currentUserId, cancellationToken);
        if (profile is null)
        {
            return SubmitFailed(
                "Doctor profile was not found.",
                DoctorDashboardFailureReason.DoctorNotFound);
        }

        if (profile.PublicProfileReviewStatus == DoctorProfileReviewStatus.Approved)
        {
            return SubmitFailed(
                "Doctor public profile is already approved.",
                DoctorDashboardFailureReason.InvalidStatus);
        }

        var missingItems = GetPublicProfileMissingItems(profile);
        if (missingItems.Count > 0)
        {
            return new DoctorPublicProfileSubmitResult(
                false,
                "Doctor public profile is not ready for review.",
                MissingReadinessItems: missingItems,
                FailureReason: DoctorDashboardFailureReason.Validation);
        }

        var submitted = await repository.SubmitPublicProfileForReviewAsync(
            currentUserId,
            DateTimeOffset.UtcNow,
            cancellationToken);

        return submitted is null
            ? SubmitFailed("Doctor profile was not found.", DoctorDashboardFailureReason.DoctorNotFound)
            : new DoctorPublicProfileSubmitResult(
                true,
                "Doctor public profile submitted for Admin review.",
                ToProfileResponse(submitted));
    }

    public async Task<IReadOnlyList<DoctorAppointmentResponse>> GetOwnAppointmentRequestsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (currentUserId == Guid.Empty)
        {
            return [];
        }

        var appointments = await repository.GetRequestedAppointmentsByUserIdAsync(
            currentUserId,
            cancellationToken);

        return appointments.Select(ToAppointmentResponse).ToList();
    }

    public async Task<DoctorAppointmentActionResult> AcceptAppointmentRequestAsync(
        Guid currentUserId,
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        if (currentUserId == Guid.Empty || appointmentId == Guid.Empty)
        {
            return AppointmentActionFailed(
                "Authenticated user and appointment id are required.",
                DoctorDashboardFailureReason.Validation);
        }

        var accepted = await repository.AcceptAppointmentRequestAsync(
            currentUserId,
            appointmentId,
            cancellationToken);

        if (accepted is null)
        {
            return AppointmentActionFailed(
                "Appointment request was not found.",
                DoctorDashboardFailureReason.AppointmentNotFound);
        }

        if (accepted.Status != AppointmentStatus.PendingPayment)
        {
            return AppointmentActionFailed(
                "Invalid appointment request status transition.",
                DoctorDashboardFailureReason.InvalidStatus);
        }

        return new DoctorAppointmentActionResult(
            true,
            "Appointment request accepted. Appointment is pending payment.",
            ToAppointmentResponse(accepted));
    }

    public async Task<DoctorAppointmentActionResult> RejectAppointmentRequestAsync(
        Guid currentUserId,
        Guid appointmentId,
        string? rejectionReason,
        CancellationToken cancellationToken = default)
    {
        if (currentUserId == Guid.Empty || appointmentId == Guid.Empty)
        {
            return AppointmentActionFailed(
                "Authenticated user and appointment id are required.",
                DoctorDashboardFailureReason.Validation);
        }

        var normalizedReason = NormalizeOptional(rejectionReason);
        if (normalizedReason is null)
        {
            return AppointmentActionFailed(
                "Rejection reason is required.",
                DoctorDashboardFailureReason.Validation);
        }

        var rejected = await repository.RejectAppointmentRequestAsync(
            currentUserId,
            appointmentId,
            normalizedReason,
            cancellationToken);

        if (rejected is null)
        {
            return AppointmentActionFailed(
                "Appointment request was not found.",
                DoctorDashboardFailureReason.AppointmentNotFound);
        }

        if (rejected.Status != AppointmentStatus.Rejected)
        {
            return AppointmentActionFailed(
                "Invalid appointment request status transition.",
                DoctorDashboardFailureReason.InvalidStatus);
        }

        return new DoctorAppointmentActionResult(
            true,
            "Appointment request rejected.",
            ToAppointmentResponse(rejected));
    }

    private static DoctorProfileResult ProfileFailed(
        string message,
        DoctorDashboardFailureReason reason)
    {
        return new DoctorProfileResult(false, message, FailureReason: reason);
    }

    private static DoctorAvatarUploadResult AvatarFailed(
        string message,
        DoctorDashboardFailureReason reason)
    {
        return new DoctorAvatarUploadResult(false, message, FailureReason: reason);
    }

    private static DoctorPublicProfileSubmitResult SubmitFailed(
        string message,
        DoctorDashboardFailureReason reason)
    {
        return new DoctorPublicProfileSubmitResult(false, message, FailureReason: reason);
    }

    private static DoctorAppointmentActionResult AppointmentActionFailed(
        string message,
        DoctorDashboardFailureReason reason)
    {
        return new DoctorAppointmentActionResult(false, message, FailureReason: reason);
    }

    private static IReadOnlyList<string> GetPublicProfileMissingItems(DoctorProfileRecord profile)
    {
        var missingItems = new List<string>();

        if (profile.Status != AccountStatus.Active)
        {
            missingItems.Add("Active account status");
        }

        if (!profile.EmailConfirmed)
        {
            missingItems.Add("Confirmed email");
        }

        if (profile.SpecialtyId == Guid.Empty)
        {
            missingItems.Add("Specialty");
        }

        if (string.IsNullOrWhiteSpace(profile.PhoneNumber))
        {
            missingItems.Add("Phone number");
        }

        if (string.IsNullOrWhiteSpace(profile.Bio))
        {
            missingItems.Add("Bio");
        }

        if (string.IsNullOrWhiteSpace(profile.AvatarUrl))
        {
            missingItems.Add("Avatar/profile image");
        }

        return missingItems;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool IsAllowedImage(string extension, string contentType)
    {
        if (string.IsNullOrWhiteSpace(extension) ||
            string.IsNullOrWhiteSpace(contentType) ||
            !AllowedContentTypesByExtension.TryGetValue(extension, out var allowedContentType))
        {
            return false;
        }

        return string.Equals(contentType.Trim(), allowedContentType, StringComparison.OrdinalIgnoreCase);
    }

    private static DoctorProfileResponse ToProfileResponse(DoctorProfileRecord record)
    {
        return new DoctorProfileResponse(
            record.DoctorProfileId,
            record.UserId,
            record.FullName,
            record.Email,
            record.PhoneNumber,
            record.Status.ToString(),
            record.EmailConfirmed,
            record.SpecialtyId,
            record.SpecialtyName,
            record.Bio,
            null,
            record.PublicProfileApproved,
            record.PublicProfileReviewStatus.ToString(),
            record.SubmittedForReviewAt,
            record.ReviewedAt,
            record.ReviewedByAdminId,
            record.PublicProfileRejectionReason,
            record.AvatarUrl,
            record.AvatarUrl,
            record.CreatedAt,
            record.UpdatedAt);
    }

    private static DoctorAppointmentResponse ToAppointmentResponse(DoctorAppointmentRecord record)
    {
        return new DoctorAppointmentResponse(
            record.AppointmentId,
            record.PatientProfileId,
            record.PatientName,
            record.MedicalServiceId,
            record.MedicalServiceName,
            record.DoctorScheduleSlotId,
            record.StartTime,
            record.EndTime,
            record.Status.ToString(),
            record.PaymentStatus?.ToString(),
            record.Notes,
            record.CreatedAt);
    }
}
