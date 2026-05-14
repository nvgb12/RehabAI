namespace RehabAI.Application.Auth;

public sealed record RegisterPatientCommand(string FullName, string Email, string PhoneNumber, string Password);
public sealed record LoginCommand(string Email, string Password);
public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword);

public interface IAuthService
{
    Task RegisterPatientAsync(RegisterPatientCommand command, CancellationToken cancellationToken = default);
    Task<string> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default);
    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default);
}
