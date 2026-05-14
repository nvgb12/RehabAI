namespace RehabAI.Api.Contracts.Auth;

public sealed record RegisterPatientRequest(string FullName, string Email, string? PhoneNumber, string Password);
public sealed record VerifyEmailRequest(string Email, string Token);
public sealed record LoginRequest(string Email, string Password);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
