namespace RehabAI.Api.Contracts.Doctors;

public sealed record UpdateDoctorSelfProfileRequest
{
    public string? PhoneNumber { get; init; }
    public string? Bio { get; init; }
    public int? YearsOfExperience { get; init; }
}

public sealed record UploadDoctorAvatarRequest
{
    public IFormFile? File { get; init; }
}

public sealed record RejectAppointmentRequestReviewRequest
{
    public string? RejectionReason { get; init; }
}
