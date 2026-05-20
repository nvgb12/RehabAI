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
