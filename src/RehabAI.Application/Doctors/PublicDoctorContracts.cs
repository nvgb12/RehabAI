namespace RehabAI.Application.Doctors;

public sealed record PublicDoctorSearchQuery(
    string? Keyword,
    Guid? SpecialtyId,
    DateTimeOffset? AvailableFrom,
    DateTimeOffset? AvailableTo);

public sealed record PublicDoctorSummaryResponse(
    Guid DoctorProfileId,
    Guid UserId,
    string FullName,
    Guid SpecialtyId,
    string SpecialtyName,
    string? Bio,
    string? AvatarUrl,
    DateTimeOffset NextAvailableSlotStartTime,
    DateTimeOffset NextAvailableSlotEndTime);

public sealed record PublicDoctorSearchResult(
    bool Succeeded,
    string Message,
    IReadOnlyList<PublicDoctorSummaryResponse> Doctors,
    PublicDoctorSearchFailureReason? FailureReason = null);

public enum PublicDoctorSearchFailureReason
{
    Validation = 1
}

public interface IPublicDoctorListingService
{
    Task<PublicDoctorSearchResult> SearchAsync(PublicDoctorSearchQuery query, CancellationToken cancellationToken = default);
    Task<PublicDoctorSummaryResponse?> GetByIdAsync(Guid doctorProfileId, CancellationToken cancellationToken = default);
}

public interface IPublicDoctorListingRepository
{
    Task<IReadOnlyList<PublicDoctorRecord>> SearchAsync(
        PublicDoctorSearchCriteria criteria,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<PublicDoctorRecord?> GetByIdAsync(Guid doctorProfileId, DateTimeOffset now, CancellationToken cancellationToken = default);
}

public sealed record PublicDoctorSearchCriteria(
    string? Keyword,
    Guid? SpecialtyId,
    DateTimeOffset? AvailableFrom,
    DateTimeOffset? AvailableTo);

public sealed record PublicDoctorRecord(
    Guid DoctorProfileId,
    Guid UserId,
    string FullName,
    Guid SpecialtyId,
    string SpecialtyName,
    string? Bio,
    string? AvatarUrl,
    DateTimeOffset NextAvailableSlotStartTime,
    DateTimeOffset NextAvailableSlotEndTime);
