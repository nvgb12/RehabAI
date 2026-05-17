using RehabAI.Application.MedicalServices;

namespace RehabAI.UnitTests.MedicalServices;

public class MedicalServiceManagerTests
{
    [Fact]
    public async Task Create_WithValidCommandAndMissingCurrency_DefaultsCurrencyToVnd()
    {
        var repository = new FakeMedicalServiceRepository();
        var manager = new MedicalServiceManager(repository);

        var result = await manager.CreateAsync(new UpsertMedicalServiceCommand(
            "Initial Rehabilitation Consultation",
            "First consultation for rehabilitation planning.",
            60,
            300000,
            null,
            true,
            false,
            null));

        Assert.True(result.Succeeded);
        Assert.Equal("VND", result.MedicalService!.Currency);
        Assert.Equal("VND", repository.CreatedDraft!.Currency);
    }

    public static TheoryData<string, int, decimal, decimal?> InvalidCreateCommands => new()
    {
        { "", 60, 300000m, null },
        { "Consultation", 0, 300000m, null },
        { "Consultation", 60, -1m, null },
        { "Consultation", 60, 300000m, -1m }
    };

    [Theory]
    [MemberData(nameof(InvalidCreateCommands))]
    public async Task Create_WithInvalidCommand_ReturnsValidationFailure(
        string name,
        int durationMinutes,
        decimal price,
        decimal? noShowFeeAmount)
    {
        var manager = new MedicalServiceManager(new FakeMedicalServiceRepository());

        var result = await manager.CreateAsync(new UpsertMedicalServiceCommand(
            name,
            null,
            durationMinutes,
            price,
            "VND",
            true,
            noShowFeeAmount is not null,
            noShowFeeAmount));

        Assert.False(result.Succeeded);
        Assert.Equal(MedicalServiceFailureReason.Validation, result.FailureReason);
    }

    [Fact]
    public async Task Update_WhenServiceDoesNotExist_ReturnsNotFound()
    {
        var manager = new MedicalServiceManager(new FakeMedicalServiceRepository());

        var result = await manager.UpdateAsync(Guid.NewGuid(), ValidCommand());

        Assert.False(result.Succeeded);
        Assert.Equal(MedicalServiceFailureReason.NotFound, result.FailureReason);
    }

    [Fact]
    public async Task SoftDelete_WhenServiceExists_ReturnsSuccess()
    {
        var repository = new FakeMedicalServiceRepository();
        var service = await repository.CreateAsync(new MedicalServiceDraft(
            "Consultation",
            null,
            60,
            300000,
            "VND",
            true,
            false,
            null));
        var manager = new MedicalServiceManager(repository);

        var result = await manager.SoftDeleteAsync(service.Id);

        Assert.True(result.Succeeded);
        Assert.Contains(service.Id, repository.DeletedIds);
    }

    private static UpsertMedicalServiceCommand ValidCommand()
    {
        return new UpsertMedicalServiceCommand(
            "Initial Rehabilitation Consultation",
            "First consultation for rehabilitation planning.",
            60,
            300000,
            "VND",
            true,
            false,
            null);
    }

    private sealed class FakeMedicalServiceRepository : IMedicalServiceRepository
    {
        private readonly Dictionary<Guid, MedicalServiceRecord> services = [];

        public MedicalServiceDraft? CreatedDraft { get; private set; }
        public List<Guid> DeletedIds { get; } = [];

        public Task<IReadOnlyList<MedicalServiceRecord>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(services.Values.Where(service => service.IsActive).ToList() as IReadOnlyList<MedicalServiceRecord>);
        }

        public Task<MedicalServiceRecord?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            services.TryGetValue(id, out var service);

            return Task.FromResult(service is { IsActive: true } ? service : null);
        }

        public Task<MedicalServiceRecord> CreateAsync(
            MedicalServiceDraft draft,
            CancellationToken cancellationToken = default)
        {
            CreatedDraft = draft;
            var record = new MedicalServiceRecord(
                Guid.NewGuid(),
                draft.Name,
                draft.Description,
                draft.DurationMinutes,
                draft.Price,
                draft.Currency,
                draft.IsActive,
                draft.NoShowFeeEnabled,
                draft.NoShowFeeAmount);

            services[record.Id] = record;

            return Task.FromResult(record);
        }

        public Task<MedicalServiceRecord?> UpdateAsync(
            Guid id,
            MedicalServiceDraft draft,
            CancellationToken cancellationToken = default)
        {
            if (!services.ContainsKey(id))
            {
                return Task.FromResult<MedicalServiceRecord?>(null);
            }

            var record = new MedicalServiceRecord(
                id,
                draft.Name,
                draft.Description,
                draft.DurationMinutes,
                draft.Price,
                draft.Currency,
                draft.IsActive,
                draft.NoShowFeeEnabled,
                draft.NoShowFeeAmount);

            services[id] = record;

            return Task.FromResult<MedicalServiceRecord?>(record);
        }

        public Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (!services.ContainsKey(id))
            {
                return Task.FromResult(false);
            }

            DeletedIds.Add(id);

            return Task.FromResult(true);
        }
    }
}
