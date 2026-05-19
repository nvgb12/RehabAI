namespace RehabAI.Application.PatientProfiles;

public sealed class PatientProfileService(IPatientProfileRepository repository) : IPatientProfileService
{
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

        var normalizedCommand = new UpdatePatientProfileCommand(
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

    private static PatientProfileResult Failed(string message, PatientProfileFailureReason reason)
    {
        return new PatientProfileResult(false, message, FailureReason: reason);
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
            record.Address);
    }
}
