namespace RehabAI.Api.Contracts.Doctors;

public sealed class RejectDoctorPublicProfileRequest
{
    public string RejectionReason { get; set; } = string.Empty;
}
