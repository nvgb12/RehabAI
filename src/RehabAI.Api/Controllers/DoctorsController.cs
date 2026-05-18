using Microsoft.AspNetCore.Mvc;
using RehabAI.Api.Contracts.Doctors;
using RehabAI.Application.DoctorSchedules;
using RehabAI.Application.Doctors;
using RehabAI.Domain.Enums;

namespace RehabAI.Api.Controllers;

[ApiController]
[Route("api/doctors")]
public class DoctorsController(
    IPublicDoctorListingService publicDoctorListingService,
    IDoctorScheduleSlotService scheduleSlotService) : ControllerBase
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

    [HttpGet("{doctorProfileId:guid}/schedule-slots")]
    public async Task<IActionResult> GetScheduleSlots(Guid doctorProfileId, CancellationToken cancellationToken)
    {
        var slots = await scheduleSlotService.GetDoctorSlotsAsync(doctorProfileId, cancellationToken);

        return Ok(slots);
    }

    [HttpGet("{doctorProfileId:guid}/available-slots")]
    public async Task<IActionResult> GetAvailableSlots(Guid doctorProfileId, CancellationToken cancellationToken)
    {
        var slots = await scheduleSlotService.GetAvailableSlotsAsync(doctorProfileId, cancellationToken);

        return Ok(slots);
    }

    [HttpPost("{doctorProfileId:guid}/schedule-slots")]
    public async Task<IActionResult> CreateScheduleSlot(
        Guid doctorProfileId,
        CreateDoctorScheduleSlotRequest request,
        CancellationToken cancellationToken)
    {
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

    [HttpPut("{doctorProfileId:guid}/schedule-slots/{slotId:guid}")]
    public async Task<IActionResult> UpdateScheduleSlot(
        Guid doctorProfileId,
        Guid slotId,
        UpdateDoctorScheduleSlotRequest request,
        CancellationToken cancellationToken)
    {
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

    [HttpDelete("{doctorProfileId:guid}/schedule-slots/{slotId:guid}")]
    public async Task<IActionResult> DisableScheduleSlot(
        Guid doctorProfileId,
        Guid slotId,
        CancellationToken cancellationToken)
    {
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
}
