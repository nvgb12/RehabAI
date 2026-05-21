using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RehabAI.Api.Contracts.Auth;
using RehabAI.Application.Auth;

namespace RehabAI.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class AuthController(IAuthService authService, IHostEnvironment hostEnvironment) : ControllerBase
{
    [HttpPost("register-patient")]
    public async Task<IActionResult> RegisterPatient(RegisterPatientRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterPatientAsync(
            new RegisterPatientCommand(request.FullName, request.Email, request.PhoneNumber, request.Password),
            cancellationToken);

        if (result.Succeeded)
        {
            var response = new Dictionary<string, object?>
            {
                ["message"] = result.Message,
                ["userId"] = result.UserId,
                ["email"] = result.Email
            };

            if (hostEnvironment.IsDevelopment())
            {
                response["verificationToken"] = result.VerificationToken;
                response["swaggerVerifyEmailRequest"] = new
                {
                    email = result.Email,
                    token = result.VerificationToken
                };
            }

            return Accepted(response);
        }

        return result.FailureReason switch
        {
            RegisterPatientFailureReason.DuplicateEmail => Conflict(new { message = result.Message, email = result.Email }),
            RegisterPatientFailureReason.MissingPatientRole => Problem(result.Message, statusCode: StatusCodes.Status500InternalServerError),
            RegisterPatientFailureReason.EmailDeliveryFailed => Problem(result.Message, statusCode: StatusCodes.Status502BadGateway),
            _ => BadRequest(new { message = result.Message })
        };
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.VerifyEmailAsync(
            new VerifyEmailCommand(request.Email, request.Token),
            cancellationToken);

        if (result.Succeeded)
        {
            return Ok(new
            {
                message = result.Message,
                userId = result.UserId,
                email = result.Email
            });
        }

        return result.FailureReason switch
        {
            VerifyEmailFailureReason.ExpiredToken => StatusCode(StatusCodes.Status410Gone, new { message = result.Message, email = result.Email }),
            VerifyEmailFailureReason.UsedToken => Conflict(new { message = result.Message, email = result.Email }),
            _ => BadRequest(new { message = result.Message, email = result.Email })
        };
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(
            new LoginCommand(request.Email, request.Password),
            cancellationToken);

        if (result.Succeeded)
        {
            return Ok(new
            {
                message = result.Message,
                userId = result.UserId,
                email = result.Email,
                fullName = result.FullName,
                roles = result.Roles,
                accessToken = result.AccessToken,
                patientProfileId = result.PatientProfileId
            });
        }

        return result.FailureReason switch
        {
            LoginFailureReason.InvalidCredentials => Unauthorized(new { message = result.Message }),
            LoginFailureReason.AccountBlocked => StatusCode(StatusCodes.Status403Forbidden, new { message = result.Message, email = result.Email }),
            _ => BadRequest(new { message = result.Message })
        };
    }

    [HttpPost("setup-doctor-password")]
    public async Task<IActionResult> SetupDoctorPassword(SetupDoctorPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.SetupDoctorPasswordAsync(
            new SetupDoctorPasswordCommand(request.Email, request.Token, request.Password),
            cancellationToken);

        if (result.Succeeded)
        {
            return Ok(new
            {
                message = result.Message,
                email = result.Email
            });
        }

        return result.FailureReason switch
        {
            SetupDoctorPasswordFailureReason.ExpiredToken => StatusCode(StatusCodes.Status410Gone, new { message = result.Message, email = result.Email }),
            SetupDoctorPasswordFailureReason.UsedToken => Conflict(new { message = result.Message, email = result.Email }),
            _ => BadRequest(new { message = result.Message, email = result.Email })
        };
    }

    [HttpPost("forgot-password")]
    public IActionResult ForgotPassword(ForgotPasswordRequest request)
    {
        return Accepted(new { message = "UC-21 scaffolded: reset email will be sent if account is eligible.", request.Email });
    }

    [HttpPost("reset-password")]
    public IActionResult ResetPassword(ResetPasswordRequest request)
    {
        return Accepted(new { message = "UC-21 scaffolded: validate token and update password.", request.Email });
    }
}
