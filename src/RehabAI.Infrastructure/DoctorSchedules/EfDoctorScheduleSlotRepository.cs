using Microsoft.EntityFrameworkCore;
using RehabAI.Application.DoctorSchedules;
using RehabAI.Domain.Entities;
using RehabAI.Domain.Enums;
using RehabAI.Infrastructure.Database;

namespace RehabAI.Infrastructure.DoctorSchedules;

public sealed class EfDoctorScheduleSlotRepository(AppDbContext dbContext) : IDoctorScheduleSlotRepository
{
    public async Task<DoctorProfileScheduleState?> GetDoctorProfileScheduleStateAsync(
        Guid doctorProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.DoctorProfiles
            .Where(profile => profile.Id == doctorProfileId && !profile.IsDeleted)
            .Select(profile => new DoctorProfileScheduleState(
                profile.Id,
                profile.UserId,
                profile.User != null && profile.User.Status == AccountStatus.Active && !profile.User.IsDeleted,
                profile.User != null &&
                    profile.User.Roles.Any(userRole => userRole.Role != null && userRole.Role.Name == "Doctor")))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DoctorScheduleSlotRecord>> GetDoctorSlotsAsync(
        Guid doctorProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.DoctorScheduleSlots
            .Where(slot => slot.DoctorProfileId == doctorProfileId && !slot.IsDeleted)
            .OrderBy(slot => slot.StartTime)
            .Select(slot => ToRecord(slot))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DoctorScheduleSlotRecord>> GetAvailableSlotsAsync(
        Guid doctorProfileId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.DoctorScheduleSlots
            .Where(slot =>
                slot.DoctorProfileId == doctorProfileId &&
                !slot.IsDeleted &&
                slot.Status == ScheduleSlotStatus.Available &&
                slot.StartTime > now)
            .OrderBy(slot => slot.StartTime)
            .Select(slot => ToRecord(slot))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasOverlappingSlotAsync(
        Guid doctorProfileId,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        Guid? excludedSlotId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.DoctorScheduleSlots.AnyAsync(
            slot =>
                slot.DoctorProfileId == doctorProfileId &&
                !slot.IsDeleted &&
                slot.Status != ScheduleSlotStatus.Disabled &&
                (!excludedSlotId.HasValue || slot.Id != excludedSlotId.Value) &&
                slot.StartTime < endTime &&
                startTime < slot.EndTime,
            cancellationToken);
    }

    public Task<bool> SlotHasActiveAppointmentsAsync(Guid slotId, CancellationToken cancellationToken = default)
    {
        return dbContext.Appointments.AnyAsync(
            appointment =>
                appointment.DoctorScheduleSlotId.HasValue &&
                appointment.DoctorScheduleSlotId.Value == slotId &&
                !appointment.IsDeleted &&
                (appointment.Status == AppointmentStatus.PendingPayment ||
                    appointment.Status == AppointmentStatus.Pending ||
                    appointment.Status == AppointmentStatus.Confirmed),
            cancellationToken);
    }

    public async Task<DoctorScheduleSlotRecord?> CreateAsync(
        DoctorScheduleSlotDraft draft,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var slot = new DoctorScheduleSlot
        {
            DoctorProfileId = draft.DoctorProfileId,
            StartTime = draft.StartTime,
            EndTime = draft.EndTime,
            Status = ScheduleSlotStatus.Available,
            ReservedUntil = null,
            CreatedByUserId = draft.CreatedByUserId,
            CreatedAt = now
        };

        dbContext.DoctorScheduleSlots.Add(slot);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToRecord(slot);
    }

    public async Task<DoctorScheduleSlotRecord?> UpdateAsync(
        DoctorScheduleSlotDraft draft,
        CancellationToken cancellationToken = default)
    {
        if (!draft.SlotId.HasValue)
        {
            return null;
        }

        var slot = await dbContext.DoctorScheduleSlots
            .SingleOrDefaultAsync(
                slot =>
                    slot.Id == draft.SlotId.Value &&
                    slot.DoctorProfileId == draft.DoctorProfileId &&
                    !slot.IsDeleted,
                cancellationToken);

        if (slot is null)
        {
            return null;
        }

        slot.StartTime = draft.StartTime;
        slot.EndTime = draft.EndTime;
        slot.Status = draft.Status;
        slot.ReservedUntil = draft.Status is ScheduleSlotStatus.Available or ScheduleSlotStatus.Disabled
            ? null
            : draft.ReservedUntil;
        slot.UpdatedByUserId = draft.UpdatedByUserId;
        slot.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToRecord(slot);
    }

    public async Task<DoctorScheduleSlotRecord?> DisableAsync(
        Guid doctorProfileId,
        Guid slotId,
        Guid? updatedByUserId,
        CancellationToken cancellationToken = default)
    {
        var slot = await dbContext.DoctorScheduleSlots
            .SingleOrDefaultAsync(
                slot =>
                    slot.Id == slotId &&
                    slot.DoctorProfileId == doctorProfileId &&
                    !slot.IsDeleted,
                cancellationToken);

        if (slot is null)
        {
            return null;
        }

        slot.Status = ScheduleSlotStatus.Disabled;
        slot.ReservedUntil = null;
        slot.UpdatedByUserId = updatedByUserId;
        slot.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToRecord(slot);
    }

    private static DoctorScheduleSlotRecord ToRecord(DoctorScheduleSlot slot)
    {
        return new DoctorScheduleSlotRecord(
            slot.Id,
            slot.DoctorProfileId,
            slot.StartTime,
            slot.EndTime,
            slot.Status,
            slot.ReservedUntil,
            slot.CreatedByUserId,
            slot.UpdatedByUserId);
    }
}
