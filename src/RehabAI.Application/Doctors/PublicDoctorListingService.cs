namespace RehabAI.Application.Doctors;

public sealed class PublicDoctorListingService(IPublicDoctorListingRepository repository) : IPublicDoctorListingService
{
    public async Task<PublicDoctorSearchResult> SearchAsync(
        PublicDoctorSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationMessage = ValidateQuery(query);

        if (validationMessage is not null)
        {
            return new PublicDoctorSearchResult(
                false,
                validationMessage,
                [],
                PublicDoctorSearchFailureReason.Validation);
        }

        var criteria = new PublicDoctorSearchCriteria(
            NormalizeOptional(query.Keyword),
            query.SpecialtyId,
            query.AvailableFrom,
            query.AvailableTo);

        var doctors = await repository.SearchAsync(criteria, DateTimeOffset.UtcNow, cancellationToken);

        return new PublicDoctorSearchResult(
            true,
            "Public doctors retrieved successfully.",
            doctors.Select(ToResponse).ToList());
    }

    public async Task<PublicDoctorSummaryResponse?> GetByIdAsync(
        Guid doctorProfileId,
        CancellationToken cancellationToken = default)
    {
        if (doctorProfileId == Guid.Empty)
        {
            return null;
        }

        var doctor = await repository.GetByIdAsync(doctorProfileId, DateTimeOffset.UtcNow, cancellationToken);

        return doctor is null ? null : ToResponse(doctor);
    }

    private static string? ValidateQuery(PublicDoctorSearchQuery query)
    {
        if (query.SpecialtyId == Guid.Empty)
        {
            return "Specialty id is invalid.";
        }

        if (query.AvailableFrom.HasValue &&
            query.AvailableTo.HasValue &&
            query.AvailableFrom.Value >= query.AvailableTo.Value)
        {
            return "Available from must be before available to.";
        }

        return null;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static PublicDoctorSummaryResponse ToResponse(PublicDoctorRecord record)
    {
        return new PublicDoctorSummaryResponse(
            record.DoctorProfileId,
            record.UserId,
            record.FullName,
            record.SpecialtyId,
            record.SpecialtyName,
            record.Bio,
            record.AvatarUrl,
            record.NextAvailableSlotStartTime,
            record.NextAvailableSlotEndTime);
    }
}
