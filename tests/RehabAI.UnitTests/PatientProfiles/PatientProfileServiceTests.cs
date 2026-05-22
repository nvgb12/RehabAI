using RehabAI.Application.PatientProfiles;

namespace RehabAI.UnitTests.PatientProfiles;

public class PatientProfileServiceTests
{
    [Fact]
    public async Task GetProfileAsync_WhenProfileExists_ReturnsSafeProfileFields()
    {
        var repository = new FakePatientProfileRepository();
        var service = new PatientProfileService(repository, new FakeProfileImageStorage());

        var profile = await service.GetProfileAsync(repository.PatientProfileId);

        Assert.NotNull(profile);
        Assert.Equal(repository.PatientProfileId, profile.PatientProfileId);
        Assert.Equal(repository.UserId, profile.UserId);
        Assert.Equal("Stroke Rehab Patient", profile.FullName);
        Assert.Equal("patient@test.com", profile.Email);
        Assert.Equal("0912345678", profile.PhoneNumber);
    }

    [Fact]
    public async Task GetProfileAsync_WhenProfileDoesNotExist_ReturnsNull()
    {
        var repository = new FakePatientProfileRepository();
        var service = new PatientProfileService(repository, new FakeProfileImageStorage());

        var profile = await service.GetProfileAsync(Guid.NewGuid());

        Assert.Null(profile);
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenProfileExists_UpdatesBasicProfileFields()
    {
        var repository = new FakePatientProfileRepository();
        var service = new PatientProfileService(repository, new FakeProfileImageStorage());
        var dateOfBirth = new DateOnly(1990, 5, 20);

        var result = await service.UpdateProfileAsync(
            repository.PatientProfileId,
            new UpdatePatientProfileCommand(
                " Updated Stroke Rehab Patient ",
                " 0987654321 ",
                dateOfBirth,
                " Female ",
                " Stroke rehabilitation home address "));

        Assert.True(result.Succeeded);
        Assert.Equal("Updated Stroke Rehab Patient", result.Profile!.FullName);
        Assert.Equal("0987654321", result.Profile.PhoneNumber);
        Assert.Equal(dateOfBirth, result.Profile!.DateOfBirth);
        Assert.Equal("Female", result.Profile.Gender);
        Assert.Equal("Stroke rehabilitation home address", result.Profile.Address);
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenFullNameIsBlank_ReturnsValidationFailure()
    {
        var repository = new FakePatientProfileRepository();
        var service = new PatientProfileService(repository, new FakeProfileImageStorage());

        var result = await service.UpdateProfileAsync(
            repository.PatientProfileId,
            new UpdatePatientProfileCommand(
                " ",
                "0912345678",
                null,
                null,
                null));

        Assert.False(result.Succeeded);
        Assert.Equal(PatientProfileFailureReason.Validation, result.FailureReason);
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenProfileDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakePatientProfileRepository();
        var service = new PatientProfileService(repository, new FakeProfileImageStorage());

        var result = await service.UpdateProfileAsync(
            Guid.NewGuid(),
            new UpdatePatientProfileCommand(
                "Stroke Rehab Patient",
                "0912345678",
                new DateOnly(1990, 5, 20),
                "Female",
                "Stroke rehabilitation home address"));

        Assert.False(result.Succeeded);
        Assert.Equal(PatientProfileFailureReason.NotFound, result.FailureReason);
    }

    [Fact]
    public async Task UploadProfileImageAsync_WhenValidImage_SavesUrl()
    {
        var repository = new FakePatientProfileRepository();
        var storage = new FakeProfileImageStorage();
        var service = new PatientProfileService(repository, storage);

        var result = await service.UploadProfileImageAsync(new UploadPatientProfileImageCommand(
            repository.UserId,
            "avatar.png",
            "image/png",
            128,
            new MemoryStream([1, 2, 3])));

        Assert.True(result.Succeeded);
        Assert.Equal("/uploads/profile-images/test.png", result.ProfileImageUrl);
        Assert.Equal("/uploads/profile-images/test.png", repository.ProfileImageUrl);
        Assert.Equal(".png", storage.Extension);
    }

    [Fact]
    public async Task UploadProfileImageAsync_WhenExtensionIsInvalid_ReturnsValidationFailure()
    {
        var repository = new FakePatientProfileRepository();
        var service = new PatientProfileService(repository, new FakeProfileImageStorage());

        var result = await service.UploadProfileImageAsync(new UploadPatientProfileImageCommand(
            repository.UserId,
            "avatar.gif",
            "image/gif",
            128,
            new MemoryStream([1, 2, 3])));

        Assert.False(result.Succeeded);
        Assert.Equal(PatientProfileFailureReason.Validation, result.FailureReason);
    }

    [Fact]
    public async Task UploadProfileImageAsync_WhenFileIsTooLarge_ReturnsFileTooLarge()
    {
        var repository = new FakePatientProfileRepository();
        var service = new PatientProfileService(repository, new FakeProfileImageStorage());

        var result = await service.UploadProfileImageAsync(new UploadPatientProfileImageCommand(
            repository.UserId,
            "avatar.jpg",
            "image/jpeg",
            2 * 1024 * 1024 + 1,
            new MemoryStream([1, 2, 3])));

        Assert.False(result.Succeeded);
        Assert.Equal(PatientProfileFailureReason.FileTooLarge, result.FailureReason);
    }

    private sealed class FakePatientProfileRepository : IPatientProfileRepository
    {
        private PatientProfileRecord profile;

        public FakePatientProfileRepository()
        {
            PatientProfileId = Guid.NewGuid();
            UserId = Guid.NewGuid();
            profile = new PatientProfileRecord(
                PatientProfileId,
                UserId,
                "Stroke Rehab Patient",
                "patient@test.com",
                "0912345678",
                null,
                null,
                null,
                null);
        }

        public Guid PatientProfileId { get; }
        public Guid UserId { get; }
        public string? ProfileImageUrl => profile.ProfileImageUrl;

        public Task<PatientProfileRecord?> GetByIdAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(patientProfileId == PatientProfileId ? profile : null);
        }

        public Task<PatientProfileRecord?> UpdateAsync(
            Guid patientProfileId,
            UpdatePatientProfileCommand command,
            CancellationToken cancellationToken = default)
        {
            if (patientProfileId != PatientProfileId)
            {
                return Task.FromResult<PatientProfileRecord?>(null);
            }

            profile = profile with
            {
                FullName = command.FullName!,
                PhoneNumber = command.PhoneNumber,
                DateOfBirth = command.DateOfBirth,
                Gender = command.Gender,
                Address = command.Address
            };

            return Task.FromResult<PatientProfileRecord?>(profile);
        }

        public Task<PatientProfileRecord?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(userId == UserId ? profile : null);
        }

        public Task<string?> UpdateProfileImageAsync(
            Guid patientProfileId,
            string profileImageUrl,
            CancellationToken cancellationToken = default)
        {
            if (patientProfileId != PatientProfileId)
            {
                return Task.FromResult<string?>(null);
            }

            profile = profile with { ProfileImageUrl = profileImageUrl };

            return Task.FromResult<string?>(profileImageUrl);
        }
    }

    private sealed class FakeProfileImageStorage : IProfileImageStorage
    {
        public string? Extension { get; private set; }

        public Task<string> SaveAsync(
            Stream content,
            string fileExtension,
            CancellationToken cancellationToken = default)
        {
            Extension = fileExtension;
            return Task.FromResult($"/uploads/profile-images/test{fileExtension}");
        }
    }
}
