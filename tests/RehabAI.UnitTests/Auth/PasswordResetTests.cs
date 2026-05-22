using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using RehabAI.Api.Contracts.Auth;
using RehabAI.Api.Controllers;
using RehabAI.Application.Auth;
using RehabAI.Application.Emails;

namespace RehabAI.UnitTests.Auth;

public class PasswordResetTests
{
    [Fact]
    public async Task RequestPasswordResetAsync_WithEligibleUser_CreatesTokenAndEmailLog()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeAuthRepository
        {
            PasswordResetUser = new PasswordResetUserRecord(userId, "doctor@example.com", "Dr Stroke Rehab")
        };
        var service = CreateService(repository, new FakeTokenService());

        var result = await service.RequestPasswordResetAsync(
            new RequestPasswordResetCommand("doctor@example.com", IncludeDevelopmentPayload: true));

        Assert.True(result.Succeeded);
        Assert.NotNull(repository.CreatedPasswordReset);
        Assert.Equal(userId, repository.CreatedPasswordReset.UserId);
        Assert.Equal("sha256:raw-reset-token", repository.CreatedPasswordReset.TokenHash);
        Assert.True(repository.CreatedPasswordReset.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.Contains("raw-reset-token", repository.CreatedPasswordReset.DevelopmentPayloadJson);
        Assert.True(repository.EmailWasMarkedSent);
    }

    [Fact]
    public async Task ForgotPassword_ReturnsGenericAcceptedResponse()
    {
        var controller = CreateController(CreateService(new FakeAuthRepository(), new FakeTokenService()));

        var response = await controller.ForgotPassword(
            new ForgotPasswordRequest("unknown@example.com"),
            CancellationToken.None);

        Assert.IsType<AcceptedResult>(response);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_UpdatesPasswordAndUsesToken()
    {
        var userId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        var tokenService = new FakeTokenService();
        var repository = new FakeAuthRepository
        {
            PasswordResetTokens =
            [
                new PasswordResetTokenRecord(
                    userId,
                    tokenId,
                    "doctor@example.com",
                    tokenService.HashToken("raw-reset-token"),
                    DateTimeOffset.UtcNow.AddMinutes(15),
                    null)
            ]
        };
        var service = CreateService(repository, tokenService);

        var result = await service.ResetPasswordAsync(
            new ResetPasswordCommand("doctor@example.com", "raw-reset-token", "NewPassword@123"));

        Assert.True(result.Succeeded);
        Assert.Equal(userId, repository.CompletedPasswordResetUserId);
        Assert.Equal(tokenId, repository.CompletedPasswordResetTokenId);
        Assert.Equal("hashed:NewPassword@123", repository.CompletedPasswordResetPasswordHash);
    }

    [Fact]
    public async Task ResetPassword_WithReusedToken_ReturnsConflict()
    {
        var tokenService = new FakeTokenService();
        var repository = new FakeAuthRepository
        {
            PasswordResetTokens =
            [
                new PasswordResetTokenRecord(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "doctor@example.com",
                    tokenService.HashToken("raw-reset-token"),
                    DateTimeOffset.UtcNow.AddMinutes(15),
                    DateTimeOffset.UtcNow)
            ]
        };
        var controller = CreateController(CreateService(repository, tokenService));

        var response = await controller.ResetPassword(
            new ResetPasswordRequest("doctor@example.com", "raw-reset-token", "NewPassword@123"),
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(response);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_ReturnsBadRequest()
    {
        var tokenService = new FakeTokenService();
        var repository = new FakeAuthRepository
        {
            PasswordResetTokens =
            [
                new PasswordResetTokenRecord(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "doctor@example.com",
                    tokenService.HashToken("different-token"),
                    DateTimeOffset.UtcNow.AddMinutes(15),
                    null)
            ]
        };
        var controller = CreateController(CreateService(repository, tokenService));

        var response = await controller.ResetPassword(
            new ResetPasswordRequest("doctor@example.com", "raw-reset-token", "NewPassword@123"),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(response);
    }

    [Fact]
    public async Task ResetPassword_WithExpiredToken_ReturnsGone()
    {
        var tokenService = new FakeTokenService();
        var repository = new FakeAuthRepository
        {
            PasswordResetTokens =
            [
                new PasswordResetTokenRecord(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "doctor@example.com",
                    tokenService.HashToken("raw-reset-token"),
                    DateTimeOffset.UtcNow.AddMinutes(-1),
                    null)
            ]
        };
        var controller = CreateController(CreateService(repository, tokenService));

        var response = await controller.ResetPassword(
            new ResetPasswordRequest("doctor@example.com", "raw-reset-token", "NewPassword@123"),
            CancellationToken.None);
        var objectResult = Assert.IsType<ObjectResult>(response);

        Assert.Equal(410, objectResult.StatusCode);
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
        public PasswordResetUserRecord? PasswordResetUser { get; init; }
        public IReadOnlyList<PasswordResetTokenRecord> PasswordResetTokens { get; init; } = [];
        public PendingPasswordReset? CreatedPasswordReset { get; private set; }
        public bool EmailWasMarkedSent { get; private set; }
        public Guid? CompletedPasswordResetUserId { get; private set; }
        public Guid? CompletedPasswordResetTokenId { get; private set; }
        public string? CompletedPasswordResetPasswordHash { get; private set; }

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
            return Task.FromResult(
                PasswordResetUser is not null && PasswordResetUser.Email == normalizedEmail
                    ? PasswordResetUser
                    : null);
        }

        public Task<Guid?> CreatePasswordResetAsync(
            PendingPasswordReset reset,
            CancellationToken cancellationToken = default)
        {
            CreatedPasswordReset = reset;
            return Task.FromResult<Guid?>(Guid.NewGuid());
        }

        public Task<IReadOnlyList<PasswordResetTokenRecord>> GetPasswordResetTokensAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PasswordResetTokenRecord>>(
                PasswordResetTokens.Where(token => token.Email == normalizedEmail).ToList());
        }

        public Task CompletePasswordResetAsync(
            Guid userId,
            Guid tokenId,
            string passwordHash,
            CancellationToken cancellationToken = default)
        {
            CompletedPasswordResetUserId = userId;
            CompletedPasswordResetTokenId = tokenId;
            CompletedPasswordResetPasswordHash = passwordHash;
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

        public Task MarkEmailSentAsync(Guid emailLogId, CancellationToken cancellationToken = default)
        {
            EmailWasMarkedSent = true;
            return Task.CompletedTask;
        }

        public Task MarkEmailFailedAsync(
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
            return "raw-reset-token";
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
            return "unit-test-access-token";
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
