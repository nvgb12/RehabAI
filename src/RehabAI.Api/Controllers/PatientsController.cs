using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RehabAI.Api.Authorization;
using RehabAI.Api.Contracts.Patients;
using RehabAI.Application.PatientProfiles;

namespace RehabAI.Api.Controllers;

[ApiController]
[Authorize(Policy = AccessPolicies.ActivePatient)]
[Route("api/patients")]
public class PatientsController(IPatientProfileService patientProfileService) : ControllerBase
{
    [HttpGet("{patientProfileId:guid}/profile")]
    public async Task<IActionResult> GetProfile(Guid patientProfileId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        var profile = await patientProfileService.GetProfileAsync(patientProfileId, cancellationToken);

        if (profile is null)
        {
            return NotFound(new { message = "Patient profile was not found." });
        }

        return profile.UserId == currentUserId.Value
            ? Ok(profile)
            : Forbid();
    }

    [HttpPut("{patientProfileId:guid}/profile")]
    public async Task<IActionResult> UpdateProfile(
        Guid patientProfileId,
        [FromBody] UpdatePatientProfileRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        var existingProfile = await patientProfileService.GetProfileAsync(patientProfileId, cancellationToken);

        if (existingProfile is null)
        {
            return NotFound(new { message = "Patient profile was not found." });
        }

        if (existingProfile.UserId != currentUserId.Value)
        {
            return Forbid();
        }

        var result = await patientProfileService.UpdateProfileAsync(
            patientProfileId,
            new UpdatePatientProfileCommand(
                request.FullName,
                request.PhoneNumber,
                request.DateOfBirth,
                request.Gender,
                request.Address),
            cancellationToken);

        if (!result.Succeeded)
        {
            return result.FailureReason switch
            {
                PatientProfileFailureReason.NotFound => NotFound(new { message = result.Message }),
                _ => BadRequest(new { message = result.Message })
            };
        }

        return Ok(new
        {
            message = result.Message,
            profile = result.Profile
        });
    }

    [HttpPost("me/profile-image")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadProfileImage(
        [FromForm] UploadPatientProfileImageRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        if (request.File is null)
        {
            return BadRequest(new { message = "Profile image file is required." });
        }

        await using var content = request.File.OpenReadStream();
        var result = await patientProfileService.UploadProfileImageAsync(
            new UploadPatientProfileImageCommand(
                currentUserId.Value,
                request.File.FileName,
                request.File.ContentType,
                request.File.Length,
                content),
            cancellationToken);

        if (!result.Succeeded)
        {
            return result.FailureReason switch
            {
                PatientProfileFailureReason.NotFound => NotFound(new { message = result.Message }),
                PatientProfileFailureReason.FileTooLarge => BadRequest(new { message = result.Message }),
                _ => BadRequest(new { message = result.Message })
            };
        }

        return Ok(new { result.ProfileImageUrl });
    }

    private Guid? GetCurrentUserId()
    {
        var claimValue =
            User.FindFirstValue("sub") ??
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }
}
