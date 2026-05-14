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

    [HttpPost("appointments")]
    public IActionResult BookAppointment() => Accepted(new { message = "UC-07 scaffolded: book appointment with soft reservation/payment rules." });

    [HttpGet("appointments")]
    public IActionResult GetAppointments() => Ok(Array.Empty<object>());

    [HttpPost("appointments/{id:guid}/confirm")]
    public IActionResult ConfirmAppointment(Guid id) => Accepted(new { message = "UC-15 scaffolded: confirm appointment.", id });

    [HttpPost("appointments/{id:guid}/cancel")]
    public IActionResult CancelAppointment(Guid id) => Accepted(new { message = "UC-08/UC-15 scaffolded: cancel appointment with policy checks.", id });
}
