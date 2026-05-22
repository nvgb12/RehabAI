using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using RehabAI.Api.Contracts.Auth;
using RehabAI.Api.Controllers;
using RehabAI.Application.Auth;
using RehabAI.Application.Emails;

namespace RehabAI.UnitTests.Auth;

public class EmailVerificationTests
{
    [Fact]
    public async Task VerifyEmailAsync_WithValidToken_VerifiesSuccessfully()
    {
        var userId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        var tokenService = new FakeTokenService();
        var repository = new FakePatientRegistrationRepository
        {
            Tokens =
            [
                new EmailVerificationTokenRecord(
                    userId,
                    tokenId,
                    "patient@example.com",
                    tokenService.HashToken("raw-token"),
                    DateTimeOffset.UtcNow.AddHours(1),
                    null)
            ]
        };
        var service = CreateService(repository, tokenService);

        var result = await service.VerifyEmailAsync(new VerifyEmailCommand("patient@example.com", "raw-token"));

        Assert.True(result.Succeeded);
        Assert.Equal(userId, repository.CompletedUserId);
        Assert.Equal(tokenId, repository.CompletedTokenId);
    }

    [Fact]
    public async Task VerifyEmail_WithReusedToken_Returns409()
    {
        var tokenService = new FakeTokenService();
        var repository = new FakePatientRegistrationRepository
        {
            Tokens =
            [
                new EmailVerificationTokenRecord(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "patient@example.com",
                    tokenService.HashToken("raw-token"),
                    DateTimeOffset.UtcNow.AddHours(1),
                    DateTimeOffset.UtcNow)
            ]
        };
        var controller = CreateController(CreateService(repository, tokenService));

        var response = await controller.VerifyEmail(new VerifyEmailRequest("patient@example.com", "raw-token"), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(response);
    }

    [Fact]
    public async Task VerifyEmail_WithInvalidToken_Returns400()
    {
        var tokenService = new FakeTokenService();
        var repository = new FakePatientRegistrationRepository
        {
            Tokens =
            [
                new EmailVerificationTokenRecord(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "patient@example.com",
                    tokenService.HashToken("different-token"),
                    DateTimeOffset.UtcNow.AddHours(1),
                    null)
            ]
        };
        var controller = CreateController(CreateService(repository, tokenService));

        var response = await controller.VerifyEmail(new VerifyEmailRequest("patient@example.com", "raw-token"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(response);
    }

    [Fact]
    public async Task VerifyEmail_WithExpiredToken_Returns410()
    {
        var tokenService = new FakeTokenService();
        var repository = new FakePatientRegistrationRepository
        {
            Tokens =
            [
                new EmailVerificationTokenRecord(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "patient@example.com",
                    tokenService.HashToken("raw-token"),
                    DateTimeOffset.UtcNow.AddHours(-1),
                    null)
            ]
        };
        var controller = CreateController(CreateService(repository, tokenService));

        var response = await controller.VerifyEmail(new VerifyEmailRequest("patient@example.com", "raw-token"), CancellationToken.None);
        var objectResult = Assert.IsType<ObjectResult>(response);

        Assert.Equal(StatusCodes.Status410Gone, objectResult.StatusCode);
    }

    private static AuthService CreateService(
        FakePatientRegistrationRepository repository,
        ISecureTokenService tokenService)
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

    private sealed class FakePatientRegistrationRepository :
        IPatientRegistrationRepository,
        IUserAuthenticationRepository
    {
        public IReadOnlyList<EmailVerificationTokenRecord> Tokens { get; init; } = [];
        public Guid? CompletedUserId { get; private set; }
        public Guid? CompletedTokenId { get; private set; }

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
            return Task.FromResult(Tokens.Where(token => token.Email == normalizedEmail).ToList() as IReadOnlyList<EmailVerificationTokenRecord>);
        }

        public Task CompleteEmailVerificationAsync(Guid userId, Guid tokenId, CancellationToken cancellationToken = default)
        {
            CompletedUserId = userId;
            CompletedTokenId = tokenId;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<DoctorInvitationTokenRecord>> GetDoctorInvitationTokensAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DoctorInvitationTokenRecord>>([]);
        }

        public Task CompleteDoctorPasswordSetupAsync(
            Guid userId,
            Guid tokenId,
            string passwordHash,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<PasswordResetUserRecord?> GetEligiblePasswordResetUserAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PasswordResetUserRecord?>(null);
        }

        public Task<Guid?> CreatePasswordResetAsync(
            PendingPasswordReset reset,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Guid?>(Guid.NewGuid());
        }

        public Task<IReadOnlyList<PasswordResetTokenRecord>> GetPasswordResetTokensAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PasswordResetTokenRecord>>([]);
        }

        public Task CompletePasswordResetAsync(
            Guid userId,
            Guid tokenId,
            string passwordHash,
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

        public Task MarkEmailSentAsync(Guid emailLogId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task MarkEmailFailedAsync(
            Guid emailLogId,
            string errorMessage,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
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
