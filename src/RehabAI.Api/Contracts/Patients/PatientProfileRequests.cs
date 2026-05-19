namespace RehabAI.Api.Contracts.Patients;

public sealed record UpdatePatientProfileRequest
{
    public DateOnly? DateOfBirth { get; init; }
    public string? Gender { get; init; }
    public string? Address { get; init; }
}
