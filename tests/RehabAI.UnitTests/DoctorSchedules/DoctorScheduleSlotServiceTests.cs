using RehabAI.Application.DoctorSchedules;
using RehabAI.Domain.Enums;

namespace RehabAI.UnitTests.DoctorSchedules;

public class DoctorScheduleSlotServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidFutureSlot_CreatesAvailableSlot()
    {
        var repository = new FakeDoctorScheduleSlotRepository();
        var service = new DoctorScheduleSlotService(repository);
        var start = DateTimeOffset.UtcNow.AddDays(2);
        var end = start.AddHours(1);

        var result = await service.CreateAsync(new CreateDoctorScheduleSlotCommand(repository.DoctorProfileId, start, end));

        Assert.True(result.Succeeded);
        Assert.Equal(nameof(ScheduleSlotStatus.Available), result.Slot!.Status);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidTime_ReturnsValidationFailure()
    {
        var repository = new FakeDoctorScheduleSlotRepository();
        var service = new DoctorScheduleSlotService(repository);
        var start = DateTimeOffset.UtcNow.AddDays(2);

        var result = await service.CreateAsync(new CreateDoctorScheduleSlotCommand(repository.DoctorProfileId, start, start));

        Assert.False(result.Succeeded);
        Assert.Equal(DoctorScheduleSlotFailureReason.Validation, result.FailureReason);
    }

    [Fact]
    public async Task CreateAsync_WhenSlotOverlaps_ReturnsConflict()
    {
        var repository = new FakeDoctorScheduleSlotRepository();
        var service = new DoctorScheduleSlotService(repository);
        var start = DateTimeOffset.UtcNow.AddDays(2);
        await service.CreateAsync(new CreateDoctorScheduleSlotCommand(repository.DoctorProfileId, start, start.AddHours(1)));

        var result = await service.CreateAsync(new CreateDoctorScheduleSlotCommand(
            repository.DoctorProfileId,
            start.AddMinutes(30),
            start.AddHours(2)));

        Assert.False(result.Succeeded);
        Assert.Equal(DoctorScheduleSlotFailureReason.Overlap, result.FailureReason);
    }

    [Fact]
    public async Task UpdateAsync_WithValidSlot_UpdatesSlot()
    {
        var repository = new FakeDoctorScheduleSlotRepository();
        var service = new DoctorScheduleSlotService(repository);
        var start = DateTimeOffset.UtcNow.AddDays(2);
        var created = await service.CreateAsync(new CreateDoctorScheduleSlotCommand(repository.DoctorProfileId, start, start.AddHours(1)));
        var updatedStart = start.AddHours(2);

        var result = await service.UpdateAsync(new UpdateDoctorScheduleSlotCommand(
            repository.DoctorProfileId,
            created.Slot!.Id,
            updatedStart,
            updatedStart.AddHours(1),
            ScheduleSlotStatus.Available));

        Assert.True(result.Succeeded);
        Assert.Equal(updatedStart, result.Slot!.StartTime);
    }

    [Fact]
    public async Task DisableAsync_WhenSlotHasActiveAppointments_ReturnsConflict()
    {
        var repository = new FakeDoctorScheduleSlotRepository();
        var service = new DoctorScheduleSlotService(repository);
        var start = DateTimeOffset.UtcNow.AddDays(2);
        var created = await service.CreateAsync(new CreateDoctorScheduleSlotCommand(repository.DoctorProfileId, start, start.AddHours(1)));
        repository.SlotsWithActiveAppointments.Add(created.Slot!.Id);

        var result = await service.DisableAsync(new DisableDoctorScheduleSlotCommand(repository.DoctorProfileId, created.Slot.Id));

        Assert.False(result.Succeeded);
        Assert.Equal(DoctorScheduleSlotFailureReason.ActiveAppointmentsExist, result.FailureReason);
    }

    [Fact]
    public async Task DisableAsync_WithValidSlot_DisablesSlot()
    {
        var repository = new FakeDoctorScheduleSlotRepository();
        var service = new DoctorScheduleSlotService(repository);
        var start = DateTimeOffset.UtcNow.AddDays(2);
        var created = await service.CreateAsync(new CreateDoctorScheduleSlotCommand(repository.DoctorProfileId, start, start.AddHours(1)));

        var result = await service.DisableAsync(new DisableDoctorScheduleSlotCommand(repository.DoctorProfileId, created.Slot!.Id));

        Assert.True(result.Succeeded);
        Assert.Equal(nameof(ScheduleSlotStatus.Disabled), result.Slot!.Status);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ExcludesDisabledSlots()
    {
        var repository = new FakeDoctorScheduleSlotRepository();
        var service = new DoctorScheduleSlotService(repository);
        var start = DateTimeOffset.UtcNow.AddDays(2);
        var created = await service.CreateAsync(new CreateDoctorScheduleSlotCommand(repository.DoctorProfileId, start, start.AddHours(1)));

        await service.DisableAsync(new DisableDoctorScheduleSlotCommand(repository.DoctorProfileId, created.Slot!.Id));

        var availableSlots = await service.GetAvailableSlotsAsync(repository.DoctorProfileId);

        Assert.Empty(availableSlots);
    }

    private sealed class FakeDoctorScheduleSlotRepository : IDoctorScheduleSlotRepository
    {
        private readonly Dictionary<Guid, DoctorScheduleSlotRecord> slots = [];

        public Guid DoctorProfileId { get; } = Guid.NewGuid();
        public List<Guid> SlotsWithActiveAppointments { get; } = [];

        public Task<DoctorProfileScheduleState?> GetDoctorProfileScheduleStateAsync(
            Guid doctorProfileId,
            CancellationToken cancellationToken = default)
        {
            if (doctorProfileId != DoctorProfileId)
            {
                return Task.FromResult<DoctorProfileScheduleState?>(null);
            }

            return Task.FromResult<DoctorProfileScheduleState?>(new DoctorProfileScheduleState(
                DoctorProfileId,
                Guid.NewGuid(),
                true,
                true));
        }

        public Task<IReadOnlyList<DoctorScheduleSlotRecord>> GetDoctorSlotsAsync(Guid doctorProfileId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                slots.Values
                    .Where(slot => slot.DoctorProfileId == doctorProfileId && slot.Status != ScheduleSlotStatus.Disabled)
                    .OrderBy(slot => slot.StartTime)
                    .ToList() as IReadOnlyList<DoctorScheduleSlotRecord>);
        }

        public Task<IReadOnlyList<DoctorScheduleSlotRecord>> GetAvailableSlotsAsync(
            Guid doctorProfileId,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                slots.Values
                    .Where(slot =>
                        slot.DoctorProfileId == doctorProfileId &&
                        slot.Status == ScheduleSlotStatus.Available &&
                        slot.StartTime > now)
                    .OrderBy(slot => slot.StartTime)
                    .ToList() as IReadOnlyList<DoctorScheduleSlotRecord>);
        }

        public Task<bool> HasOverlappingSlotAsync(
            Guid doctorProfileId,
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            Guid? excludedSlotId,
            CancellationToken cancellationToken = default)
        {
            var overlaps = slots.Values.Any(slot =>
                slot.DoctorProfileId == doctorProfileId &&
                slot.Status != ScheduleSlotStatus.Disabled &&
                (!excludedSlotId.HasValue || slot.Id != excludedSlotId.Value) &&
                slot.StartTime < endTime &&
                startTime < slot.EndTime);

            return Task.FromResult(overlaps);
        }

        public Task<bool> SlotHasActiveAppointmentsAsync(Guid slotId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SlotsWithActiveAppointments.Contains(slotId));
        }

        public Task<DoctorScheduleSlotRecord?> CreateAsync(
            DoctorScheduleSlotDraft draft,
            CancellationToken cancellationToken = default)
        {
            var record = ToRecord(draft, Guid.NewGuid());
            slots[record.Id] = record;

            return Task.FromResult<DoctorScheduleSlotRecord?>(record);
        }

        public Task<DoctorScheduleSlotRecord?> UpdateAsync(
            DoctorScheduleSlotDraft draft,
            CancellationToken cancellationToken = default)
        {
            if (!draft.SlotId.HasValue || !slots.ContainsKey(draft.SlotId.Value))
            {
                return Task.FromResult<DoctorScheduleSlotRecord?>(null);
            }

            var record = ToRecord(draft, draft.SlotId.Value);
            slots[record.Id] = record;

            return Task.FromResult<DoctorScheduleSlotRecord?>(record);
        }

        public Task<DoctorScheduleSlotRecord?> DisableAsync(
            Guid doctorProfileId,
            Guid slotId,
            Guid? updatedByUserId,
            CancellationToken cancellationToken = default)
        {
            if (!slots.TryGetValue(slotId, out var slot) || slot.DoctorProfileId != doctorProfileId)
            {
                return Task.FromResult<DoctorScheduleSlotRecord?>(null);
            }

            var disabled = slot with
            {
                Status = ScheduleSlotStatus.Disabled,
                ReservedUntil = null,
                UpdatedByUserId = updatedByUserId
            };
            slots[slotId] = disabled;

            return Task.FromResult<DoctorScheduleSlotRecord?>(disabled);
        }

        private static DoctorScheduleSlotRecord ToRecord(DoctorScheduleSlotDraft draft, Guid id)
        {
            return new DoctorScheduleSlotRecord(
                id,
                draft.DoctorProfileId,
                draft.StartTime,
                draft.EndTime,
                draft.Status,
                draft.ReservedUntil,
                draft.CreatedByUserId,
                draft.UpdatedByUserId);
        }
    }
}
