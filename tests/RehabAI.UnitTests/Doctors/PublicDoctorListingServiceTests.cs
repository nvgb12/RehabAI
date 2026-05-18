using RehabAI.Application.Doctors;

namespace RehabAI.UnitTests.Doctors;

public class PublicDoctorListingServiceTests
{
    [Fact]
    public async Task SearchAsync_WithValidQuery_ReturnsPublicDoctors()
    {
        var repository = new FakePublicDoctorListingRepository();
        var service = new PublicDoctorListingService(repository);

        var result = await service.SearchAsync(new PublicDoctorSearchQuery(" nguyen ", null, null, null));

        Assert.True(result.Succeeded);
        Assert.Single(result.Doctors);
        Assert.Equal("Dr Nguyen Van B", result.Doctors[0].FullName);
        Assert.Equal("nguyen", repository.LastCriteria!.Keyword);
    }

    [Fact]
    public async Task SearchAsync_WithInvalidAvailabilityRange_ReturnsValidationFailure()
    {
        var service = new PublicDoctorListingService(new FakePublicDoctorListingRepository());
        var from = DateTimeOffset.UtcNow.AddDays(2);
        var to = from.AddHours(-1);

        var result = await service.SearchAsync(new PublicDoctorSearchQuery(null, null, from, to));

        Assert.False(result.Succeeded);
        Assert.Equal(PublicDoctorSearchFailureReason.Validation, result.FailureReason);
    }

    [Fact]
    public async Task GetByIdAsync_WithEmptyId_ReturnsNull()
    {
        var repository = new FakePublicDoctorListingRepository();
        var service = new PublicDoctorListingService(repository);

        var result = await service.GetByIdAsync(Guid.Empty);

        Assert.Null(result);
        Assert.False(repository.GetByIdCalled);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRepositoryReturnsDoctor_ReturnsDoctorDetail()
    {
        var repository = new FakePublicDoctorListingRepository();
        var service = new PublicDoctorListingService(repository);

        var result = await service.GetByIdAsync(repository.DoctorProfileId);

        Assert.NotNull(result);
        Assert.Equal(repository.DoctorProfileId, result.DoctorProfileId);
    }

    private sealed class FakePublicDoctorListingRepository : IPublicDoctorListingRepository
    {
        public Guid DoctorProfileId { get; } = Guid.NewGuid();
        public PublicDoctorSearchCriteria? LastCriteria { get; private set; }
        public bool GetByIdCalled { get; private set; }

        public Task<IReadOnlyList<PublicDoctorRecord>> SearchAsync(
            PublicDoctorSearchCriteria criteria,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            LastCriteria = criteria;

            return Task.FromResult<IReadOnlyList<PublicDoctorRecord>>([BuildDoctorRecord(DoctorProfileId)]);
        }

        public Task<PublicDoctorRecord?> GetByIdAsync(
            Guid doctorProfileId,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            GetByIdCalled = true;

            return Task.FromResult<PublicDoctorRecord?>(
                doctorProfileId == DoctorProfileId ? BuildDoctorRecord(doctorProfileId) : null);
        }

        private static PublicDoctorRecord BuildDoctorRecord(Guid doctorProfileId)
        {
            var start = DateTimeOffset.UtcNow.AddDays(1);

            return new PublicDoctorRecord(
                doctorProfileId,
                Guid.NewGuid(),
                "Dr Nguyen Van B",
                Guid.NewGuid(),
                "Physical Therapy",
                "Rehabilitation doctor",
                null,
                start,
                start.AddHours(1));
        }
    }
}
