using RehabAI.Application.Auth;
using RehabAI.Application.Doctors;
using RehabAI.Application.Emails;
using RehabAI.Domain.Enums;

namespace RehabAI.UnitTests.Doctors;

public class AdminDoctorListServiceTests
{
    [Fact]
    public async Task GetAdminDoctorsAsync_MapsDoctorRecordsWithoutSensitiveFields()
    {
        var repository = new FakeDoctorAccountRepository();
        var service = new DoctorService(repository, new FakeSecureTokenService(), new FakeEmailSender());

        var doctors = await service.GetAdminDoctorsAsync(CancellationToken.None);

        var doctor = Assert.Single(doctors);
        Assert.Equal(repository.DoctorProfileId, doctor.DoctorProfileId);
        Assert.Equal(repository.UserId, doctor.UserId);
        Assert.Equal("Dr Nguyen Stroke Rehab", doctor.FullName);
        Assert.Equal("doctor@test.com", doctor.Email);
        Assert.Equal("PendingPasswordSetup", doctor.Status);
        Assert.False(doctor.EmailConfirmed);
        Assert.Equal(repository.SpecialtyId, doctor.SpecialtyId);
        Assert.Equal("Stroke Rehabilitation", doctor.SpecialtyName);
        Assert.False(doctor.PublicProfileApproved);
        Assert.Equal("Draft", doctor.PublicProfileReviewStatus);
        Assert.False(doctor.IsPublicProfileReady);
        Assert.Contains("Active account status", doctor.PublicProfileMissingItems);
        Assert.False(doctor.IsDeleted);
    }

    [Fact]
    public async Task ApprovePublicProfileAsync_WhenSubmitted_ApprovesProfile()
    {
        var repository = new FakeDoctorAccountRepository(
            AccountStatus.Active,
            true,
            "/uploads/doctor-avatars/doctor.png",
            DoctorProfileReviewStatus.Submitted);
        var service = new DoctorService(repository, new FakeSecureTokenService(), new FakeEmailSender());

        var result = await service.ApprovePublicProfileAsync(
            repository.DoctorProfileId,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Doctor);
        Assert.True(result.Doctor.PublicProfileApproved);
        Assert.Equal("Approved", result.Doctor.PublicProfileReviewStatus);
    }

    [Fact]
    public async Task RejectPublicProfileAsync_WhenReasonMissing_ReturnsValidationFailure()
    {
        var repository = new FakeDoctorAccountRepository(
            AccountStatus.Active,
            true,
            "/uploads/doctor-avatars/doctor.png",
            DoctorProfileReviewStatus.Submitted);
        var service = new DoctorService(repository, new FakeSecureTokenService(), new FakeEmailSender());

        var result = await service.RejectPublicProfileAsync(
            repository.DoctorProfileId,
            Guid.NewGuid(),
            " ",
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(AdminDoctorPublicProfileReviewFailureReason.Validation, result.FailureReason);
    }

    private sealed class FakeDoctorAccountRepository : IDoctorAccountRepository
    {
        private AdminDoctorRecord doctor;

        public FakeDoctorAccountRepository(
            AccountStatus status = AccountStatus.PendingPasswordSetup,
            bool emailConfirmed = false,
            string? avatarUrl = null,
            DoctorProfileReviewStatus reviewStatus = DoctorProfileReviewStatus.Draft)
        {
            doctor = new AdminDoctorRecord(
                DoctorProfileId,
                UserId,
                "Dr Nguyen Stroke Rehab",
                "doctor@test.com",
                "0912345678",
                status,
                emailConfirmed,
                SpecialtyId,
                "Stroke Rehabilitation",
                "Post-stroke rehabilitation doctor.",
                avatarUrl,
                reviewStatus == DoctorProfileReviewStatus.Approved,
                reviewStatus,
                reviewStatus == DoctorProfileReviewStatus.Submitted ? DateTimeOffset.UtcNow.AddHours(-1) : null,
                null,
                null,
                null,
                DateTimeOffset.UtcNow,
                null,
                false);
        }

        public Guid DoctorProfileId { get; } = Guid.NewGuid();
        public Guid UserId { get; } = Guid.NewGuid();
        public Guid SpecialtyId { get; } = Guid.NewGuid();

        public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<Guid?> GetRoleIdByNameAsync(string roleName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Guid?>(Guid.NewGuid());
        }

        public Task<bool> SpecialtyExistsAsync(Guid specialtyId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<decimal> GetDefaultCommissionRateAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(15m);
        }

        public Task<CreatedDoctorAccountResult> CreateDoctorAccountAsync(
            CreatedDoctorAccount account,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CreatedDoctorAccountResult(UserId, DoctorProfileId, Guid.NewGuid()));
        }

        public Task<IReadOnlyList<AdminDoctorRecord>> GetAdminDoctorsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AdminDoctorRecord>>([doctor]);
        }

        public Task<AdminDoctorRecord?> GetAdminDoctorByIdAsync(Guid doctorProfileId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AdminDoctorRecord?>(
                doctorProfileId == DoctorProfileId ? doctor : null);
        }

        public Task<AdminDoctorRecord?> ApprovePublicProfileAsync(
            Guid doctorProfileId,
            Guid adminUserId,
            DateTimeOffset reviewedAt,
            CancellationToken cancellationToken = default)
        {
            if (doctorProfileId != DoctorProfileId)
            {
                return Task.FromResult<AdminDoctorRecord?>(null);
            }

            doctor = doctor with
            {
                PublicProfileApproved = true,
                PublicProfileReviewStatus = DoctorProfileReviewStatus.Approved,
                ReviewedAt = reviewedAt,
                ReviewedByAdminId = adminUserId,
                PublicProfileRejectionReason = null
            };

            return Task.FromResult<AdminDoctorRecord?>(doctor);
        }

        public Task<AdminDoctorRecord?> RejectPublicProfileAsync(
            Guid doctorProfileId,
            Guid adminUserId,
            string rejectionReason,
            DateTimeOffset reviewedAt,
            CancellationToken cancellationToken = default)
        {
            if (doctorProfileId != DoctorProfileId)
            {
                return Task.FromResult<AdminDoctorRecord?>(null);
            }

            doctor = doctor with
            {
                PublicProfileApproved = false,
                PublicProfileReviewStatus = DoctorProfileReviewStatus.Rejected,
                ReviewedAt = reviewedAt,
                ReviewedByAdminId = adminUserId,
                PublicProfileRejectionReason = rejectionReason
            };

            return Task.FromResult<AdminDoctorRecord?>(doctor);
        }

        public Task MarkInvitationEmailSentAsync(Guid emailLogId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task MarkInvitationEmailFailedAsync(
            Guid emailLogId,
            string errorMessage,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSecureTokenService : ISecureTokenService
    {
        public string GenerateToken() => "raw-token";

        public string HashToken(string token) => $"hash:{token}";

        public bool TokenHashesEqual(string hash, string expectedHash) => hash == expectedHash;
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
