using Microsoft.AspNetCore.Mvc;
using RehabAI.Api.Contracts.Auth;

namespace RehabAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("register-patient")]
    public IActionResult RegisterPatient(RegisterPatientRequest request)
    {
        return Accepted(new { message = "UC-01 scaffolded: register patient and send verification email.", request.Email });
    }

    [HttpPost("login")]
    public IActionResult Login(LoginRequest request)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "UC-02 scaffolded: implement token/cookie auth.", request.Email });
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
