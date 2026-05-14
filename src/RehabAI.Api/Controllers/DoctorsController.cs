using Microsoft.AspNetCore.Mvc;
using RehabAI.Api.Contracts.Doctors;

namespace RehabAI.Api.Controllers;

[ApiController]
[Route("api/doctors")]
public class DoctorsController : ControllerBase
{
    [HttpGet]
    public IActionResult SearchDoctors()
    {
        return Ok(new { message = "UC-06 scaffolded: only ACTIVE, public-profile-approved doctors with available slots should be returned." });
    }

    [HttpPost]
    public IActionResult CreateDoctor(CreateDoctorRequest request)
    {
        return Accepted(new { message = "UC-31 scaffolded: admin-created doctor account with password setup invitation.", request.Email });
    }

    [HttpPost("{doctorProfileId:guid}/credentials")]
    public IActionResult UploadCredential(Guid doctorProfileId)
    {
        return Accepted(new { message = "UC-31 scaffolded: upload or record private doctor credential document metadata.", doctorProfileId });
    }

    [HttpPost("{doctorProfileId:guid}/resend-invitation")]
    public IActionResult ResendInvitation(Guid doctorProfileId)
    {
        return Accepted(new { message = "UC-31 scaffolded: resend single-use password setup invitation.", doctorProfileId });
    }
}
