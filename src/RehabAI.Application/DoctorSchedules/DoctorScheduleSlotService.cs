using RehabAI.Domain.Enums;

namespace RehabAI.Application.DoctorSchedules;

public sealed class DoctorScheduleSlotService(IDoctorScheduleSlotRepository repository) : IDoctorScheduleSlotService
{
    public async Task<IReadOnlyList<DoctorScheduleSlotResponse>> GetDoctorSlotsAsync(
        Guid doctorProfileId,
        CancellationToken cancellationToken = default)
    {
        var slots = await repository.GetDoctorSlotsAsync(doctorProfileId, cancellationToken);

        return slots.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<DoctorScheduleSlotResponse>> GetAvailableSlotsAsync(
        Guid doctorProfileId,
        CancellationToken cancellationToken = default)
    {
        var slots = await repository.GetAvailableSlotsAsync(doctorProfileId, DateTimeOffset.UtcNow, cancellationToken);

        return slots.Select(ToResponse).ToList();
    }

    public async Task<DoctorScheduleSlotResult> CreateAsync(
        CreateDoctorScheduleSlotCommand command,
        CancellationToken cancellationToken = default)
    {
        var baseValidation = await ValidateDoctorAndTimeAsync(
            command.DoctorProfileId,
            command.StartTime,
            command.EndTime,
            cancellationToken);

        if (baseValidation is not null)
        {
            return baseValidation;
        }

        if (await repository.HasOverlappingSlotAsync(command.DoctorProfileId, command.StartTime, command.EndTime, null, cancellationToken))
        {
            return Failed(
                "Schedule slot overlaps with an existing slot for this doctor.",
                DoctorScheduleSlotFailureReason.Overlap);
        }

        var draft = new DoctorScheduleSlotDraft(
            null,
            command.DoctorProfileId,
            command.StartTime,
            command.EndTime,
            ScheduleSlotStatus.Available,
            null,
            command.CreatedByUserId,
            null);

        var created = await repository.CreateAsync(draft, cancellationToken);

        return new DoctorScheduleSlotResult(
            true,
            "Doctor schedule slot created successfully.",
            ToResponse(created!));
    }

    public async Task<DoctorScheduleSlotResult> UpdateAsync(
        UpdateDoctorScheduleSlotCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.SlotId == Guid.Empty)
        {
            return Failed("Schedule slot id is required.", DoctorScheduleSlotFailureReason.Validation);
        }

        var baseValidation = await ValidateDoctorAndTimeAsync(
            command.DoctorProfileId,
            command.StartTime,
            command.EndTime,
            cancellationToken);

        if (baseValidation is not null)
        {
            return baseValidation;
        }

        if (await repository.SlotHasActiveAppointmentsAsync(command.SlotId, cancellationToken))
        {
            return Failed(
                "Schedule slot has active appointments. Cancel or reschedule affected appointments first.",
                DoctorScheduleSlotFailureReason.ActiveAppointmentsExist);
        }

        if (command.Status != ScheduleSlotStatus.Disabled &&
            await repository.HasOverlappingSlotAsync(command.DoctorProfileId, command.StartTime, command.EndTime, command.SlotId, cancellationToken))
        {
            return Failed(
                "Schedule slot overlaps with an existing slot for this doctor.",
                DoctorScheduleSlotFailureReason.Overlap);
        }

        var draft = new DoctorScheduleSlotDraft(
            command.SlotId,
            command.DoctorProfileId,
            command.StartTime,
            command.EndTime,
            command.Status,
            null,
            null,
            command.UpdatedByUserId);

        var updated = await repository.UpdateAsync(draft, cancellationToken);

        if (updated is null)
        {
            return Failed("Schedule slot was not found.", DoctorScheduleSlotFailureReason.SlotNotFound);
        }

        return new DoctorScheduleSlotResult(
            true,
            "Doctor schedule slot updated successfully.",
            ToResponse(updated));
    }

    public async Task<DoctorScheduleSlotResult> DisableAsync(
        DisableDoctorScheduleSlotCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.DoctorProfileId == Guid.Empty)
        {
            return Failed("Doctor profile is required.", DoctorScheduleSlotFailureReason.Validation);
        }

        if (command.SlotId == Guid.Empty)
        {
            return Failed("Schedule slot id is required.", DoctorScheduleSlotFailureReason.Validation);
        }

        var doctorValidation = await ValidateDoctorProfileAsync(command.DoctorProfileId, cancellationToken);

        if (doctorValidation is not null)
        {
            return doctorValidation;
        }

        if (await repository.SlotHasActiveAppointmentsAsync(command.SlotId, cancellationToken))
        {
            return Failed(
                "Schedule slot has active appointments. Cancel or reschedule affected appointments first.",
                DoctorScheduleSlotFailureReason.ActiveAppointmentsExist);
        }

        var disabled = await repository.DisableAsync(
            command.DoctorProfileId,
            command.SlotId,
            command.UpdatedByUserId,
            cancellationToken);

        if (disabled is null)
        {
            return Failed("Schedule slot was not found.", DoctorScheduleSlotFailureReason.SlotNotFound);
        }

        return new DoctorScheduleSlotResult(
            true,
            "Doctor schedule slot disabled successfully.",
            ToResponse(disabled));
    }

    private async Task<DoctorScheduleSlotResult?> ValidateDoctorAndTimeAsync(
        Guid doctorProfileId,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        CancellationToken cancellationToken)
    {
        if (doctorProfileId == Guid.Empty)
        {
            return Failed("Doctor profile is required.", DoctorScheduleSlotFailureReason.Validation);
        }

        if (startTime >= endTime)
        {
            return Failed("Start time must be before end time.", DoctorScheduleSlotFailureReason.Validation);
        }

        if (startTime <= DateTimeOffset.UtcNow)
        {
            return Failed("Start time must be in the future.", DoctorScheduleSlotFailureReason.Validation);
        }

        return await ValidateDoctorProfileAsync(doctorProfileId, cancellationToken);
    }

    private async Task<DoctorScheduleSlotResult?> ValidateDoctorProfileAsync(
        Guid doctorProfileId,
        CancellationToken cancellationToken)
    {
        var doctor = await repository.GetDoctorProfileScheduleStateAsync(doctorProfileId, cancellationToken);

        if (doctor is null)
        {
            return Failed("Doctor profile was not found.", DoctorScheduleSlotFailureReason.DoctorProfileNotFound);
        }

        if (!doctor.HasDoctorRole)
        {
            return Failed("Linked user does not have the Doctor role.", DoctorScheduleSlotFailureReason.DoctorUserNotDoctor);
        }

        if (!doctor.IsUserActive)
        {
            return Failed("Linked Doctor user must be active.", DoctorScheduleSlotFailureReason.DoctorUserNotActive);
        }

        return null;
    }

    private static DoctorScheduleSlotResult Failed(string message, DoctorScheduleSlotFailureReason reason)
    {
        return new DoctorScheduleSlotResult(false, message, FailureReason: reason);
    }

    private static DoctorScheduleSlotResponse ToResponse(DoctorScheduleSlotRecord record)
    {
        return new DoctorScheduleSlotResponse(
            record.Id,
            record.DoctorProfileId,
            record.StartTime,
            record.EndTime,
            record.Status.ToString(),
            record.ReservedUntil,
            record.CreatedByUserId,
            record.UpdatedByUserId);
    }
}
