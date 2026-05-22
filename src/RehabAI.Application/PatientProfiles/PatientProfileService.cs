using System.IO;

namespace RehabAI.Application.PatientProfiles;

public sealed class PatientProfileService(
    IPatientProfileRepository repository,
    IProfileImageStorage profileImageStorage) : IPatientProfileService
{
    private const long MaxProfileImageBytes = 2 * 1024 * 1024;
    private static readonly Dictionary<string, string> AllowedContentTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };

    public async Task<PatientProfileResponse?> GetProfileAsync(
        Guid patientProfileId,
        CancellationToken cancellationToken = default)
    {
        if (patientProfileId == Guid.Empty)
        {
            return null;
        }

        var profile = await repository.GetByIdAsync(patientProfileId, cancellationToken);

        return profile is null ? null : ToResponse(profile);
    }

    public async Task<PatientProfileResult> UpdateProfileAsync(
        Guid patientProfileId,
        UpdatePatientProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        if (patientProfileId == Guid.Empty)
        {
            return Failed("Patient profile id is required.", PatientProfileFailureReason.Validation);
        }

        if (string.IsNullOrWhiteSpace(command.FullName))
        {
            return Failed("Full name is required.", PatientProfileFailureReason.Validation);
        }

        var normalizedCommand = new UpdatePatientProfileCommand(
            command.FullName.Trim(),
            NormalizeOptional(command.PhoneNumber),
            command.DateOfBirth,
            NormalizeOptional(command.Gender),
            NormalizeOptional(command.Address));

        var updated = await repository.UpdateAsync(patientProfileId, normalizedCommand, cancellationToken);

        if (updated is null)
        {
            return Failed("Patient profile was not found.", PatientProfileFailureReason.NotFound);
        }

        return new PatientProfileResult(
            true,
            "Patient profile updated successfully.",
            ToResponse(updated));
    }

    public async Task<PatientProfileImageUploadResult> UploadProfileImageAsync(
        UploadPatientProfileImageCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty)
        {
            return ImageUploadFailed("Authenticated user is required.", PatientProfileFailureReason.Validation);
        }

        if (command.Length <= 0)
        {
            return ImageUploadFailed("Profile image file is required.", PatientProfileFailureReason.Validation);
        }

        if (command.Length > MaxProfileImageBytes)
        {
            return ImageUploadFailed("Profile image must be 2MB or smaller.", PatientProfileFailureReason.FileTooLarge);
        }

        var extension = Path.GetExtension(command.FileName);
        if (!IsAllowedImage(extension, command.ContentType))
        {
            return ImageUploadFailed("Profile image must be a JPG, JPEG, PNG, or WEBP file.", PatientProfileFailureReason.Validation);
        }

        var profile = await repository.GetByUserIdAsync(command.UserId, cancellationToken);
        if (profile is null)
        {
            return ImageUploadFailed("Patient profile was not found.", PatientProfileFailureReason.NotFound);
        }

        var imageUrl = await profileImageStorage.SaveAsync(command.Content, extension, cancellationToken);
        var savedUrl = await repository.UpdateProfileImageAsync(profile.PatientProfileId, imageUrl, cancellationToken);

        return savedUrl is null
            ? ImageUploadFailed("Patient profile was not found.", PatientProfileFailureReason.NotFound)
            : new PatientProfileImageUploadResult(true, "Patient profile image uploaded successfully.", savedUrl);
    }

    private static PatientProfileResult Failed(string message, PatientProfileFailureReason reason)
    {
        return new PatientProfileResult(false, message, FailureReason: reason);
    }

    private static PatientProfileImageUploadResult ImageUploadFailed(
        string message,
        PatientProfileFailureReason reason)
    {
        return new PatientProfileImageUploadResult(false, message, FailureReason: reason);
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

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static PatientProfileResponse ToResponse(PatientProfileRecord record)
    {
        return new PatientProfileResponse(
            record.PatientProfileId,
            record.UserId,
            record.FullName,
            record.Email,
            record.PhoneNumber,
            record.DateOfBirth,
            record.Gender,
            record.Address,
            record.ProfileImageUrl);
    }
}
