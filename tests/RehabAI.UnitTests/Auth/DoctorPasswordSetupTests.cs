using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using RehabAI.Api.Contracts.Auth;
using RehabAI.Api.Controllers;
using RehabAI.Application.Auth;
using RehabAI.Application.Emails;

namespace RehabAI.UnitTests.Auth;

public class DoctorPasswordSetupTests
{
    [Fact]
    public async Task SetupDoctorPassword_WithValidToken_ActivatesDoctor()
    {
        var userId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        var tokenService = new FakeTokenService();
        var repository = new FakeAuthRepository
        {
            DoctorInvitationTokens =
            [
                new DoctorInvitationTokenRecord(
                    userId,
                    tokenId,
                    "doctor@example.com",
                    tokenService.HashToken("raw-token"),
                    DateTimeOffset.UtcNow.AddHours(1),
                    null)
            ]
        };
        var service = CreateService(repository, tokenService);

        var result = await service.SetupDoctorPasswordAsync(
            new SetupDoctorPasswordCommand("doctor@example.com", "raw-token", "Password@123"));

        Assert.True(result.Succeeded);
        Assert.Equal(userId, repository.CompletedDoctorUserId);
        Assert.Equal(tokenId, repository.CompletedDoctorTokenId);
        Assert.Equal("hashed:Password@123", repository.CompletedDoctorPasswordHash);
    }

    [Fact]
    public async Task SetupDoctorPassword_WithReusedToken_Returns409()
    {
        var tokenService = new FakeTokenService();
        var repository = new FakeAuthRepository
        {
            DoctorInvitationTokens =
            [
                new DoctorInvitationTokenRecord(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "doctor@example.com",
                    tokenService.HashToken("raw-token"),
                    DateTimeOffset.UtcNow.AddHours(1),
                    DateTimeOffset.UtcNow)
            ]
        };
        var controller = CreateController(CreateService(repository, tokenService));

        var response = await controller.SetupDoctorPassword(
            new SetupDoctorPasswordRequest("doctor@example.com", "raw-token", "Password@123"),
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(response);
    }

    [Fact]
    public async Task SetupDoctorPassword_WithInvalidToken_Returns400()
    {
        var tokenService = new FakeTokenService();
        var repository = new FakeAuthRepository
        {
            DoctorInvitationTokens =
            [
                new DoctorInvitationTokenRecord(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "doctor@example.com",
                    tokenService.HashToken("different-token"),
                    DateTimeOffset.UtcNow.AddHours(1),
                    null)
            ]
        };
        var controller = CreateController(CreateService(repository, tokenService));

        var response = await controller.SetupDoctorPassword(
            new SetupDoctorPasswordRequest("doctor@example.com", "raw-token", "Password@123"),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(response);
    }

    [Fact]
    public async Task SetupDoctorPassword_WithExpiredToken_Returns410()
    {
        var tokenService = new FakeTokenService();
        var repository = new FakeAuthRepository
        {
            DoctorInvitationTokens =
            [
                new DoctorInvitationTokenRecord(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "doctor@example.com",
                    tokenService.HashToken("raw-token"),
                    DateTimeOffset.UtcNow.AddHours(-1),
                    null)
            ]
        };
        var controller = CreateController(CreateService(repository, tokenService));

        var response = await controller.SetupDoctorPassword(
            new SetupDoctorPasswordRequest("doctor@example.com", "raw-token", "Password@123"),
            CancellationToken.None);
        var objectResult = Assert.IsType<ObjectResult>(response);

        Assert.Equal(StatusCodes.Status410Gone, objectResult.StatusCode);
    }

    private static AuthService CreateService(FakeAuthRepository repository, ISecureTokenService tokenService)
    {
        return new AuthService(
            repository,
            repository,
            new FakePasswordHasher(),
            tokenService,
            new FakeJwtTokenService(),
            new FakeEmailSender());
    }

    private static AuthController CreateController(IAuthService authService)
    {
        return new AuthController(authService, new FakeHostEnvironment());
    }

    private sealed class FakeAuthRepository :
        IPatientRegistrationRepository,
        IUserAuthenticationRepository
    {
        public IReadOnlyList<DoctorInvitationTokenRecord> DoctorInvitationTokens { get; init; } = [];
        public Guid? CompletedDoctorUserId { get; private set; }
        public Guid? CompletedDoctorTokenId { get; private set; }
        public string? CompletedDoctorPasswordHash { get; private set; }

        public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<Guid?> GetRoleIdByNameAsync(string roleName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Guid?>(Guid.NewGuid());
        }

        public Task<PendingPatientRegistrationResult> CreatePendingPatientAsync(
            PendingPatientRegistration registration,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PendingPatientRegistrationResult(Guid.NewGuid(), Guid.NewGuid()));
        }

        public Task<IReadOnlyList<EmailVerificationTokenRecord>> GetEmailVerificationTokensAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<EmailVerificationTokenRecord>>([]);
        }

        public Task CompleteEmailVerificationAsync(Guid userId, Guid tokenId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<DoctorInvitationTokenRecord>> GetDoctorInvitationTokensAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                DoctorInvitationTokens
                    .Where(token => token.Email == normalizedEmail)
                    .ToList() as IReadOnlyList<DoctorInvitationTokenRecord>);
        }

        public Task CompleteDoctorPasswordSetupAsync(
            Guid userId,
            Guid tokenId,
            string passwordHash,
            CancellationToken cancellationToken = default)
        {
            CompletedDoctorUserId = userId;
            CompletedDoctorTokenId = tokenId;
            CompletedDoctorPasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task MarkVerificationEmailSentAsync(Guid emailLogId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task MarkVerificationEmailFailedAsync(
            Guid emailLogId,
            string errorMessage,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<UserAuthenticationRecord?> GetUserForLoginAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<UserAuthenticationRecord?>(null);
        }
    }

    private sealed class FakeTokenService : ISecureTokenService
    {
        public string GenerateToken()
        {
            return "raw-token";
        }

        public string HashToken(string token)
        {
            return $"sha256:{token}";
        }

        public bool TokenHashesEqual(string hash, string expectedHash)
        {
            return hash == expectedHash;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            return $"hashed:{password}";
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            return passwordHash == HashPassword(password);
        }
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public string CreateAccessToken(UserAuthenticationRecord user)
        {
            return $"token:{user.UserId}";
        }
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "RehabAI.UnitTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
