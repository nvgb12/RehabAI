using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RehabAI.Api.Authorization;
using RehabAI.Api.Contracts.Doctors;
using RehabAI.Application.DoctorSchedules;
using RehabAI.Application.Doctors;
using RehabAI.Domain.Enums;

namespace RehabAI.Api.Controllers;

[ApiController]
[Route("api/doctors")]
public class DoctorsController(
    IPublicDoctorListingService publicDoctorListingService,
    IDoctorDashboardService doctorDashboardService,
    IDoctorScheduleSlotService scheduleSlotService,
    IEndpointAccessService accessService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> SearchDoctors(
        [FromQuery] string? keyword,
        [FromQuery] Guid? specialtyId,
        [FromQuery] DateTimeOffset? availableFrom,
        [FromQuery] DateTimeOffset? availableTo,
        CancellationToken cancellationToken)
    {
        var result = await publicDoctorListingService.SearchAsync(
            new PublicDoctorSearchQuery(keyword, specialtyId, availableFrom, availableTo),
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(result.Doctors);
    }

    [HttpGet("{doctorProfileId:guid}")]
    public async Task<IActionResult> GetDoctor(Guid doctorProfileId, CancellationToken cancellationToken)
    {
        var doctor = await publicDoctorListingService.GetByIdAsync(doctorProfileId, cancellationToken);

        return doctor is null
            ? NotFound(new { message = "Doctor was not found or is not publicly bookable." })
            : Ok(doctor);
    }

    [Authorize(Policy = AccessPolicies.ActiveDoctor)]
    [HttpGet("me/profile")]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        var profile = await doctorDashboardService.GetOwnProfileAsync(
            currentUserId.Value,
            cancellationToken);

        return profile is null
            ? NotFound(new { message = "Doctor profile was not found." })
            : Ok(profile);
    }

    [Authorize(Policy = AccessPolicies.ActiveDoctor)]
    [HttpPut("me/profile")]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateDoctorSelfProfileRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        var result = await doctorDashboardService.UpdateOwnProfileAsync(
            currentUserId.Value,
            new UpdateDoctorProfileCommand(request.PhoneNumber, request.Bio),
            cancellationToken);

        if (!result.Succeeded)
        {
            return ToDoctorDashboardErrorResponse(result.Message, result.FailureReason);
        }

        return Ok(new
        {
            message = result.Message,
            profile = result.Profile
        });
    }

    [Authorize(Policy = AccessPolicies.ActiveDoctor)]
    [HttpGet("me/appointments")]
    public async Task<IActionResult> GetMyAppointments(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        var appointments = await doctorDashboardService.GetOwnAppointmentsAsync(
            currentUserId.Value,
            cancellationToken);

        return Ok(appointments);
    }

    [Authorize(Policy = AccessPolicies.ActiveDoctor)]
    [HttpGet("me/appointments/{appointmentId:guid}")]
    public async Task<IActionResult> GetMyAppointment(
        Guid appointmentId,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        var appointment = await doctorDashboardService.GetOwnAppointmentByIdAsync(
            currentUserId.Value,
            appointmentId,
            cancellationToken);

        return appointment is null
            ? NotFound(new { message = "Appointment was not found." })
            : Ok(appointment);
    }

    [Authorize(Policy = AccessPolicies.ActiveDoctor)]
    [HttpGet("me/appointment-requests")]
    public async Task<IActionResult> GetMyAppointmentRequests(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        var appointments = await doctorDashboardService.GetOwnAppointmentRequestsAsync(
            currentUserId.Value,
            cancellationToken);

        return Ok(appointments);
    }

    [Authorize(Policy = AccessPolicies.ActiveDoctor)]
    [HttpPost("me/appointments/{appointmentId:guid}/accept")]
    public async Task<IActionResult> AcceptMyAppointmentRequest(
        Guid appointmentId,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        var result = await doctorDashboardService.AcceptAppointmentRequestAsync(
            currentUserId.Value,
            appointmentId,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ToDoctorAppointmentActionErrorResponse(result);
        }

        return Ok(new
        {
            message = result.Message,
            appointment = result.Appointment
        });
    }

    [Authorize(Policy = AccessPolicies.ActiveDoctor)]
    [HttpPost("me/appointments/{appointmentId:guid}/reject")]
    public async Task<IActionResult> RejectMyAppointmentRequest(
        Guid appointmentId,
        [FromBody] RejectAppointmentRequestReviewRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        var result = await doctorDashboardService.RejectAppointmentRequestAsync(
            currentUserId.Value,
            appointmentId,
            request.RejectionReason,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ToDoctorAppointmentActionErrorResponse(result);
        }

        return Ok(new
        {
            message = result.Message,
            appointment = result.Appointment
        });
    }

    [Authorize(Policy = AccessPolicies.ActiveDoctor)]
    [HttpGet("me/dashboard")]
    public async Task<IActionResult> GetMyDashboard(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        var dashboard = await doctorDashboardService.GetDashboardAsync(
            currentUserId.Value,
            cancellationToken);

        return dashboard is null
            ? NotFound(new { message = "Doctor profile was not found." })
            : Ok(dashboard);
    }

    [Authorize(Policy = AccessPolicies.ActiveDoctor)]
    [HttpPost("me/avatar")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadMyAvatar(
        [FromForm] UploadDoctorAvatarRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        if (request.File is null)
        {
            return BadRequest(new { message = "Doctor avatar file is required." });
        }

        await using var content = request.File.OpenReadStream();
        var result = await doctorDashboardService.UploadAvatarAsync(
            new UploadDoctorAvatarCommand(
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
                DoctorDashboardFailureReason.DoctorNotFound => NotFound(new { message = result.Message }),
                _ => BadRequest(new { message = result.Message })
            };
        }

        return Ok(new { avatarUrl = result.AvatarUrl });
    }

    [Authorize(Policy = AccessPolicies.ActiveDoctor)]
    [HttpPost("me/public-profile/submit")]
    public async Task<IActionResult> SubmitMyPublicProfileForReview(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        var result = await doctorDashboardService.SubmitPublicProfileForReviewAsync(
            currentUserId.Value,
            cancellationToken);

        if (!result.Succeeded)
        {
            return result.FailureReason switch
            {
                DoctorDashboardFailureReason.DoctorNotFound => NotFound(new { message = result.Message }),
                DoctorDashboardFailureReason.InvalidStatus => Conflict(new { message = result.Message }),
                _ => BadRequest(new
                {
                    message = result.Message,
                    missingItems = result.MissingReadinessItems ?? []
                })
            };
        }

        return Ok(new
        {
            message = result.Message,
            profile = result.Profile
        });
    }

    [Authorize(Policy = AccessPolicies.ActiveDoctorStaffOrAdmin)]
    [HttpGet("{doctorProfileId:guid}/schedule-slots")]
    public async Task<IActionResult> GetScheduleSlots(Guid doctorProfileId, CancellationToken cancellationToken)
    {
        if (!await CanManageDoctorProfileAsync(doctorProfileId, cancellationToken))
        {
            return Forbid();
        }

        var slots = await scheduleSlotService.GetDoctorSlotsAsync(doctorProfileId, cancellationToken);

        return Ok(slots);
    }

    [HttpGet("{doctorProfileId:guid}/available-slots")]
    public async Task<IActionResult> GetAvailableSlots(Guid doctorProfileId, CancellationToken cancellationToken)
    {
        var slots = await scheduleSlotService.GetAvailableSlotsAsync(doctorProfileId, cancellationToken);

        return Ok(slots);
    }

    [Authorize(Policy = AccessPolicies.ActiveDoctorStaffOrAdmin)]
    [HttpPost("{doctorProfileId:guid}/schedule-slots")]
    public async Task<IActionResult> CreateScheduleSlot(
        Guid doctorProfileId,
        CreateDoctorScheduleSlotRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanManageDoctorProfileAsync(doctorProfileId, cancellationToken))
        {
            return Forbid();
        }

        var result = await scheduleSlotService.CreateAsync(
            new CreateDoctorScheduleSlotCommand(doctorProfileId, request.StartTime, request.EndTime),
            cancellationToken);

        if (!result.Succeeded)
        {
            return ToScheduleSlotErrorResponse(result);
        }

        return CreatedAtAction(
            nameof(GetScheduleSlots),
            new { doctorProfileId },
            new
            {
                message = result.Message,
                slot = result.Slot
            });
    }

    [Authorize(Policy = AccessPolicies.ActiveDoctorStaffOrAdmin)]
    [HttpPut("{doctorProfileId:guid}/schedule-slots/{slotId:guid}")]
    public async Task<IActionResult> UpdateScheduleSlot(
        Guid doctorProfileId,
        Guid slotId,
        UpdateDoctorScheduleSlotRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanManageDoctorProfileAsync(doctorProfileId, cancellationToken))
        {
            return Forbid();
        }

        if (!Enum.TryParse<ScheduleSlotStatus>(request.Status, true, out var status))
        {
            return BadRequest(new { message = "Schedule slot status is invalid." });
        }

        var result = await scheduleSlotService.UpdateAsync(
            new UpdateDoctorScheduleSlotCommand(
                doctorProfileId,
                slotId,
                request.StartTime,
                request.EndTime,
                status),
            cancellationToken);

        if (!result.Succeeded)
        {
            return ToScheduleSlotErrorResponse(result);
        }

        return Ok(new
        {
            message = result.Message,
            slot = result.Slot
        });
    }

    [Authorize(Policy = AccessPolicies.ActiveDoctorStaffOrAdmin)]
    [HttpDelete("{doctorProfileId:guid}/schedule-slots/{slotId:guid}")]
    public async Task<IActionResult> DisableScheduleSlot(
        Guid doctorProfileId,
        Guid slotId,
        CancellationToken cancellationToken)
    {
        if (!await CanManageDoctorProfileAsync(doctorProfileId, cancellationToken))
        {
            return Forbid();
        }

        var result = await scheduleSlotService.DisableAsync(
            new DisableDoctorScheduleSlotCommand(doctorProfileId, slotId),
            cancellationToken);

        if (!result.Succeeded)
        {
            return ToScheduleSlotErrorResponse(result);
        }

        return Ok(new
        {
            message = result.Message,
            slot = result.Slot
        });
    }

    [Authorize(Policy = AccessPolicies.ActiveAdmin)]
    [HttpPost]
    public IActionResult CreateDoctor(CreateDoctorRequest request)
    {
        return Accepted(new { message = "UC-31 scaffolded: admin-created doctor account with password setup invitation.", request.Email });
    }

    [Authorize(Policy = AccessPolicies.ActiveDoctorStaffOrAdmin)]
    [HttpPost("{doctorProfileId:guid}/credentials")]
    public async Task<IActionResult> UploadCredential(Guid doctorProfileId, CancellationToken cancellationToken)
    {
        if (!await CanManageDoctorProfileAsync(doctorProfileId, cancellationToken))
        {
            return Forbid();
        }

        return Accepted(new { message = "UC-31 scaffolded: upload or record private doctor credential document metadata.", doctorProfileId });
    }

    [Authorize(Policy = AccessPolicies.ActiveDoctorStaffOrAdmin)]
    [HttpPost("{doctorProfileId:guid}/resend-invitation")]
    public async Task<IActionResult> ResendInvitation(Guid doctorProfileId, CancellationToken cancellationToken)
    {
        if (!await CanManageDoctorProfileAsync(doctorProfileId, cancellationToken))
        {
            return Forbid();
        }

        return Accepted(new { message = "UC-31 scaffolded: resend single-use password setup invitation.", doctorProfileId });
    }

    private IActionResult ToScheduleSlotErrorResponse(DoctorScheduleSlotResult result)
    {
        return result.FailureReason switch
        {
            DoctorScheduleSlotFailureReason.DoctorProfileNotFound => NotFound(new { message = result.Message }),
            DoctorScheduleSlotFailureReason.SlotNotFound => NotFound(new { message = result.Message }),
            DoctorScheduleSlotFailureReason.Overlap => Conflict(new { message = result.Message }),
            DoctorScheduleSlotFailureReason.ActiveAppointmentsExist => Conflict(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }

    private IActionResult ToDoctorDashboardErrorResponse(
        string message,
        DoctorDashboardFailureReason? failureReason)
    {
        return failureReason switch
        {
            DoctorDashboardFailureReason.DoctorNotFound => NotFound(new { message }),
            DoctorDashboardFailureReason.AppointmentNotFound => NotFound(new { message }),
            _ => BadRequest(new { message })
        };
    }

    private IActionResult ToDoctorAppointmentActionErrorResponse(DoctorAppointmentActionResult result)
    {
        return result.FailureReason switch
        {
            DoctorDashboardFailureReason.AppointmentNotFound => NotFound(new { message = result.Message }),
            DoctorDashboardFailureReason.InvalidStatus => BadRequest(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }

    private async Task<bool> CanManageDoctorProfileAsync(
        Guid doctorProfileId,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

        return currentUserId is not null &&
            await accessService.CanManageDoctorProfileAsync(
                currentUserId.Value,
                doctorProfileId,
                cancellationToken);
    }

    private Guid? GetCurrentUserId()
    {
        var claimValue =
            User.FindFirstValue("sub") ??
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }
}
