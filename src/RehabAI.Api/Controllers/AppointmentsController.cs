using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RehabAI.Api.Authorization;
using RehabAI.Api.Contracts.Appointments;
using RehabAI.Application.Appointments;

namespace RehabAI.Api.Controllers;

[ApiController]
[Authorize(Policy = AccessPolicies.ActivePatient)]
[Route("api")]
public class AppointmentsController(
    IAppointmentBookingService appointmentBookingService,
    IEndpointAccessService accessService) : ControllerBase
{
    [HttpPost("appointments")]
    public async Task<IActionResult> CreateAppointment(
        [FromBody] CreateAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        if (!await accessService.PatientProfileBelongsToUserAsync(
            currentUserId.Value,
            request.PatientProfileId,
            cancellationToken))
        {
            return Forbid();
        }

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

    [HttpPost("appointments/requests")]
    public async Task<IActionResult> CreateAppointmentRequest(
        [FromBody] CreateFlexibleAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        var patientProfileId = await accessService.GetPatientProfileIdForUserAsync(
            currentUserId.Value,
            cancellationToken);

        if (patientProfileId is null)
        {
            return Forbid();
        }

        var result = await appointmentBookingService.CreateRequestAsync(
            new CreateAppointmentRequestCommand(
                patientProfileId.Value,
                request.DoctorProfileId,
                request.MedicalServiceId,
                request.PreferredStartTime,
                request.PreferredEndTime,
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
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        if (!await accessService.AppointmentBelongsToUserAsync(currentUserId.Value, appointmentId, cancellationToken))
        {
            return NotFound(new { message = "Appointment was not found." });
        }

        var appointment = await appointmentBookingService.GetByIdAsync(appointmentId, cancellationToken);

        return appointment is null
            ? NotFound(new { message = "Appointment was not found." })
            : Ok(appointment);
    }

    [HttpPost("appointments/{appointmentId:guid}/confirm-payment")]
    public async Task<IActionResult> ConfirmPayment(Guid appointmentId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        if (!await accessService.AppointmentBelongsToUserAsync(currentUserId.Value, appointmentId, cancellationToken))
        {
            return NotFound(new { message = "Appointment was not found." });
        }

        var result = await appointmentBookingService.ConfirmPaymentAsync(appointmentId, cancellationToken);

        if (!result.Succeeded)
        {
            return ToAppointmentErrorResponse(result);
        }

        return Ok(new
        {
            message = result.Message,
            appointment = result.Appointment
        });
    }

    [HttpPost("appointments/{appointmentId:guid}/cancel")]
    public async Task<IActionResult> CancelAppointment(
        Guid appointmentId,
        [FromBody] CancelAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        if (!await accessService.AppointmentBelongsToUserAsync(currentUserId.Value, appointmentId, cancellationToken))
        {
            return NotFound(new { message = "Appointment was not found." });
        }

        var result = await appointmentBookingService.CancelAsync(
            appointmentId,
            request.CancellationReason,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ToAppointmentErrorResponse(result);
        }

        return Ok(new
        {
            message = result.Message,
            appointment = result.Appointment
        });
    }

    [HttpGet("patients/{patientProfileId:guid}/appointments")]
    public async Task<IActionResult> GetPatientAppointments(Guid patientProfileId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        if (!await accessService.PatientProfileBelongsToUserAsync(currentUserId.Value, patientProfileId, cancellationToken))
        {
            return Forbid();
        }

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
            AppointmentFailureReason.SlotNotFound => BadRequest(new { message = result.Message }),
            AppointmentFailureReason.SlotUnavailable => BadRequest(new { message = result.Message }),
            AppointmentFailureReason.DoubleBooked => BadRequest(new { message = result.Message }),
            AppointmentFailureReason.AppointmentNotFound => NotFound(new { message = result.Message }),
            AppointmentFailureReason.AppointmentNotPendingPayment => Conflict(new { message = result.Message }),
            AppointmentFailureReason.AppointmentAlreadyCancelled => Conflict(new { message = result.Message }),
            AppointmentFailureReason.AppointmentNotCancellable => Conflict(new { message = result.Message }),
            AppointmentFailureReason.PatientNotActive => StatusCode(StatusCodes.Status403Forbidden, new { message = result.Message }),
            AppointmentFailureReason.PatientRoleMissing => StatusCode(StatusCodes.Status403Forbidden, new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }

    private Guid? GetCurrentUserId()
    {
        var claimValue =
            User.FindFirstValue("sub") ??
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }
}
