using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using RehabAI.Api.Contracts.Auth;
using RehabAI.Api.Controllers;
using RehabAI.Application.Auth;
using RehabAI.Application.Emails;
using RehabAI.Domain.Enums;

namespace RehabAI.UnitTests.Auth;

public class LoginTests
{
    [Fact]
    public async Task Login_WithActiveVerifiedPatient_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeAuthRepository
        {
            User = new UserAuthenticationRecord(
                userId,
                "patient@example.com",
                "Patient Example",
                "hashed:Password@123",
                (int)AccountStatus.Active,
                ["Patient"])
        };
        var controller = CreateController(repository);

        var response = await controller.Login(new LoginRequest("patient@example.com", "Password@123"), CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(response);

        Assert.Contains("accessToken", ok.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WithPendingEmailPatient_Returns403()
    {
        var repository = new FakeAuthRepository
        {
            User = new UserAuthenticationRecord(
                Guid.NewGuid(),
                "patient@example.com",
                "Patient Example",
                "hashed:Password@123",
                (int)AccountStatus.PendingEmail,
                ["Patient"])
        };
        var controller = CreateController(repository);

        var response = await controller.Login(new LoginRequest("patient@example.com", "Password@123"), CancellationToken.None);
        var objectResult = Assert.IsType<ObjectResult>(response);

        Assert.Equal(403, objectResult.StatusCode);
        Assert.Contains("Please verify your email before logging in.", objectResult.Value!.ToString());
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var repository = new FakeAuthRepository
        {
            User = new UserAuthenticationRecord(
                Guid.NewGuid(),
                "patient@example.com",
                "Patient Example",
                "hashed:Password@123",
                (int)AccountStatus.Active,
                ["Patient"])
        };
        var controller = CreateController(repository);

        var response = await controller.Login(new LoginRequest("patient@example.com", "WrongPassword"), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(response);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var controller = CreateController(new FakeAuthRepository());

        var response = await controller.Login(new LoginRequest("unknown@example.com", "Password@123"), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(response);
    }

    private static AuthController CreateController(FakeAuthRepository repository)
    {
        var service = new AuthService(
            repository,
            repository,
            new FakePasswordHasher(),
            new FakeTokenService(),
            new FakeJwtTokenService(),
            new FakeEmailSender());

        return new AuthController(service, new FakeHostEnvironment());
    }

    private sealed class FakeAuthRepository :
        IPatientRegistrationRepository,
        IUserAuthenticationRepository
    {
        public UserAuthenticationRecord? User { get; init; }

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
            return Task.FromResult(User is not null && User.Email == normalizedEmail ? User : null);
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
