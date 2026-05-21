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
        Assert.False(doctor.IsDeleted);
    }

    private sealed class FakeDoctorAccountRepository : IDoctorAccountRepository
    {
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
            IReadOnlyList<AdminDoctorRecord> doctors =
            [
                new AdminDoctorRecord(
                    DoctorProfileId,
                    UserId,
                    "Dr Nguyen Stroke Rehab",
                    "doctor@test.com",
                    "0912345678",
                    AccountStatus.PendingPasswordSetup,
                    false,
                    SpecialtyId,
                    "Stroke Rehabilitation",
                    "Post-stroke rehabilitation doctor.",
                    false,
                    DateTimeOffset.UtcNow,
                    null,
                    false)
            ];

            return Task.FromResult(doctors);
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
