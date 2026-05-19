using Microsoft.AspNetCore.Mvc;
using RehabAI.Api.Contracts.Patients;
using RehabAI.Application.PatientProfiles;

namespace RehabAI.Api.Controllers;

[ApiController]
[Route("api/patients")]
public class PatientsController(IPatientProfileService patientProfileService) : ControllerBase
{
    [HttpGet("{patientProfileId:guid}/profile")]
    public async Task<IActionResult> GetProfile(Guid patientProfileId, CancellationToken cancellationToken)
    {
        var profile = await patientProfileService.GetProfileAsync(patientProfileId, cancellationToken);

        return profile is null
            ? NotFound(new { message = "Patient profile was not found." })
            : Ok(profile);
    }

    [HttpPut("{patientProfileId:guid}/profile")]
    public async Task<IActionResult> UpdateProfile(
        Guid patientProfileId,
        [FromBody] UpdatePatientProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await patientProfileService.UpdateProfileAsync(
            patientProfileId,
            new UpdatePatientProfileCommand(
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
}
