namespace RehabAI.Api.Contracts.Doctors;

public sealed record CreateDoctorRequest(
    string FullName,
    string Email,
    string PhoneNumber,
    Guid SpecialtyId,
    string? Bio,
    int? YearsOfExperience);
