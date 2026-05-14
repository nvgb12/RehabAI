namespace RehabAI.Application.Doctors;

public sealed record CreateDoctorCommand(string FullName, string Email, string PhoneNumber, Guid SpecialtyId, string? Bio);

public interface IDoctorService
{
    Task<Guid> CreateDoctorAsync(CreateDoctorCommand command, Guid adminUserId, CancellationToken cancellationToken = default);
    Task ResendInvitationAsync(Guid doctorProfileId, Guid adminUserId, CancellationToken cancellationToken = default);
}
