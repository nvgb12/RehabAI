using Microsoft.AspNetCore.Mvc;
using RehabAI.Api.Contracts.Appointments;
using RehabAI.Application.Appointments;

namespace RehabAI.Api.Controllers;

[ApiController]
[Route("api")]
public class AppointmentsController(IAppointmentBookingService appointmentBookingService) : ControllerBase
{
    [HttpPost("appointments")]
    public async Task<IActionResult> CreateAppointment(
        [FromBody] CreateAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await appointmentBookingService.CreateAsync(
            new CreateAppointmentCommand(
                request.PatientProfileId,
                request.DoctorProfileId,
                request.MedicalServiceId,
                request.ScheduleSlotId,
                request.Reason),
            cancellationToken);

        if (!result.Succeeded)
        {
            return ToAppointmentErrorResponse(result);
        }

        return CreatedAtAction(
            nameof(GetAppointment),
            new { appointmentId = result.Appointment!.Id },
            new
            {
                message = result.Message,
                appointment = result.Appointment
            });
    }

    [HttpGet("appointments/{appointmentId:guid}")]
    public async Task<IActionResult> GetAppointment(Guid appointmentId, CancellationToken cancellationToken)
    {
        var appointment = await appointmentBookingService.GetByIdAsync(appointmentId, cancellationToken);

        return appointment is null
            ? NotFound(new { message = "Appointment was not found." })
            : Ok(appointment);
    }

    [HttpGet("patients/{patientProfileId:guid}/appointments")]
    public async Task<IActionResult> GetPatientAppointments(Guid patientProfileId, CancellationToken cancellationToken)
    {
        var appointments = await appointmentBookingService.GetPatientAppointmentsAsync(patientProfileId, cancellationToken);

        return Ok(appointments);
    }

    private IActionResult ToAppointmentErrorResponse(AppointmentResult result)
    {
        return result.FailureReason switch
        {
            AppointmentFailureReason.PatientNotFound => NotFound(new { message = result.Message }),
            AppointmentFailureReason.DoctorNotFound => NotFound(new { message = result.Message }),
            AppointmentFailureReason.DoctorNotPublicBookable => NotFound(new { message = result.Message }),
            AppointmentFailureReason.MedicalServiceNotFound => NotFound(new { message = result.Message }),
            AppointmentFailureReason.SlotNotFound => NotFound(new { message = result.Message }),
            AppointmentFailureReason.SlotUnavailable => Conflict(new { message = result.Message }),
            AppointmentFailureReason.DoubleBooked => Conflict(new { message = result.Message }),
            AppointmentFailureReason.PatientNotActive => StatusCode(StatusCodes.Status403Forbidden, new { message = result.Message }),
            AppointmentFailureReason.PatientRoleMissing => StatusCode(StatusCodes.Status403Forbidden, new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }
}
