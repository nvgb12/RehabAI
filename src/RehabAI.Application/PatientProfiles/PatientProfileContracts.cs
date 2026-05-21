namespace RehabAI.Application.PatientProfiles;

public sealed record PatientProfileResponse(
    Guid PatientProfileId,
    Guid UserId,
    string FullName,
    string Email,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Address);

public sealed record UpdatePatientProfileCommand(
    string? FullName,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Address);

public sealed record PatientProfileResult(
    bool Succeeded,
    string Message,
    PatientProfileResponse? Profile = null,
    PatientProfileFailureReason? FailureReason = null);

public enum PatientProfileFailureReason
{
    Validation = 1,
    NotFound = 2
}

public interface IPatientProfileService
{
    Task<PatientProfileResponse?> GetProfileAsync(
        Guid patientProfileId,
        CancellationToken cancellationToken = default);

    Task<PatientProfileResult> UpdateProfileAsync(
        Guid patientProfileId,
        UpdatePatientProfileCommand command,
        CancellationToken cancellationToken = default);
}

public interface IPatientProfileRepository
{
    Task<PatientProfileRecord?> GetByIdAsync(
        Guid patientProfileId,
        CancellationToken cancellationToken = default);

    Task<PatientProfileRecord?> UpdateAsync(
        Guid patientProfileId,
        UpdatePatientProfileCommand command,
        CancellationToken cancellationToken = default);
}

public sealed record PatientProfileRecord(
    Guid PatientProfileId,
    Guid UserId,
    string FullName,
    string Email,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Address);
