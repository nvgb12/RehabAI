using Microsoft.AspNetCore.Mvc;

namespace RehabAI.Api.Controllers;

[ApiController]
[Route("api")]
public class CoreUseCasesController : ControllerBase
{
    [HttpGet("services")]
    public IActionResult GetServices() => Ok(Array.Empty<object>());

    [HttpGet("doctors/{doctorId:guid}/schedule")]
    public IActionResult GetDoctorSchedule(Guid doctorId) => Ok(new { doctorId, slots = Array.Empty<object>() });

    [HttpPost("doctors/{doctorId:guid}/schedule")]
    public IActionResult ManageDoctorSchedule(Guid doctorId) => Accepted(new { message = "UC-14 scaffolded: manage doctor schedule.", doctorId });

}
