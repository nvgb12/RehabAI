using RehabAI.Domain.Enums;

namespace RehabAI.Application.DoctorSchedules;

public sealed record DoctorScheduleSlotResponse(
    Guid Id,
    Guid DoctorProfileId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string Status,
    DateTimeOffset? ReservedUntil,
    Guid? CreatedByUserId,
    Guid? UpdatedByUserId);

public sealed record CreateDoctorScheduleSlotCommand(
    Guid DoctorProfileId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    Guid? CreatedByUserId = null);

public sealed record UpdateDoctorScheduleSlotCommand(
    Guid DoctorProfileId,
    Guid SlotId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    ScheduleSlotStatus Status,
    Guid? UpdatedByUserId = null);

public sealed record DisableDoctorScheduleSlotCommand(
    Guid DoctorProfileId,
    Guid SlotId,
    Guid? UpdatedByUserId = null);

public sealed record DoctorScheduleSlotResult(
    bool Succeeded,
    string Message,
    DoctorScheduleSlotResponse? Slot = null,
    DoctorScheduleSlotFailureReason? FailureReason = null);

public enum DoctorScheduleSlotFailureReason
{
    Validation = 1,
    DoctorProfileNotFound = 2,
    DoctorUserNotDoctor = 3,
    DoctorUserNotActive = 4,
    SlotNotFound = 5,
    Overlap = 6,
    ActiveAppointmentsExist = 7
}

public interface IDoctorScheduleSlotService
{
    Task<IReadOnlyList<DoctorScheduleSlotResponse>> GetDoctorSlotsAsync(Guid doctorProfileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoctorScheduleSlotResponse>> GetAvailableSlotsAsync(Guid doctorProfileId, CancellationToken cancellationToken = default);
    Task<DoctorScheduleSlotResult> CreateAsync(CreateDoctorScheduleSlotCommand command, CancellationToken cancellationToken = default);
    Task<DoctorScheduleSlotResult> UpdateAsync(UpdateDoctorScheduleSlotCommand command, CancellationToken cancellationToken = default);
    Task<DoctorScheduleSlotResult> DisableAsync(DisableDoctorScheduleSlotCommand command, CancellationToken cancellationToken = default);
}

public interface IDoctorScheduleSlotRepository
{
    Task<DoctorProfileScheduleState?> GetDoctorProfileScheduleStateAsync(Guid doctorProfileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoctorScheduleSlotRecord>> GetDoctorSlotsAsync(Guid doctorProfileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoctorScheduleSlotRecord>> GetAvailableSlotsAsync(Guid doctorProfileId, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingSlotAsync(Guid doctorProfileId, DateTimeOffset startTime, DateTimeOffset endTime, Guid? excludedSlotId, CancellationToken cancellationToken = default);
    Task<bool> SlotHasActiveAppointmentsAsync(Guid slotId, CancellationToken cancellationToken = default);
    Task<DoctorScheduleSlotRecord?> CreateAsync(DoctorScheduleSlotDraft draft, CancellationToken cancellationToken = default);
    Task<DoctorScheduleSlotRecord?> UpdateAsync(DoctorScheduleSlotDraft draft, CancellationToken cancellationToken = default);
    Task<DoctorScheduleSlotRecord?> DisableAsync(Guid doctorProfileId, Guid slotId, Guid? updatedByUserId, CancellationToken cancellationToken = default);
}

public sealed record DoctorProfileScheduleState(
    Guid DoctorProfileId,
    Guid UserId,
    bool IsUserActive,
    bool HasDoctorRole);

public sealed record DoctorScheduleSlotDraft(
    Guid? SlotId,
    Guid DoctorProfileId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    ScheduleSlotStatus Status,
    DateTimeOffset? ReservedUntil,
    Guid? CreatedByUserId,
    Guid? UpdatedByUserId);

public sealed record DoctorScheduleSlotRecord(
    Guid Id,
    Guid DoctorProfileId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    ScheduleSlotStatus Status,
    DateTimeOffset? ReservedUntil,
    Guid? CreatedByUserId,
    Guid? UpdatedByUserId);
