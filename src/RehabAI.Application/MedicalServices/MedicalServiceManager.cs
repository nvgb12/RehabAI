namespace RehabAI.Application.MedicalServices;

public sealed class MedicalServiceManager(IMedicalServiceRepository repository) : IMedicalServiceManager
{
    private const string DefaultCurrency = "VND";

    public async Task<IReadOnlyList<MedicalServiceResponse>> GetActiveMedicalServicesAsync(
        CancellationToken cancellationToken = default)
    {
        var services = await repository.GetActiveAsync(cancellationToken);

        return services.Select(ToResponse).ToList();
    }

    public async Task<MedicalServiceResponse?> GetActiveMedicalServiceByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var service = await repository.GetActiveByIdAsync(id, cancellationToken);

        return service is null ? null : ToResponse(service);
    }

    public async Task<MedicalServiceResult> CreateAsync(
        UpsertMedicalServiceCommand command,
        CancellationToken cancellationToken = default)
    {
        var draftResult = BuildDraft(command);

        if (!draftResult.Succeeded)
        {
            return ValidationFailed(draftResult.Message);
        }

        var created = await repository.CreateAsync(draftResult.Draft!, cancellationToken);

        return new MedicalServiceResult(
            true,
            "Medical service created successfully.",
            ToResponse(created));
    }

    public async Task<MedicalServiceResult> UpdateAsync(
        Guid id,
        UpsertMedicalServiceCommand command,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return new MedicalServiceResult(
                false,
                "Medical service id is required.",
                FailureReason: MedicalServiceFailureReason.Validation);
        }

        var draftResult = BuildDraft(command);

        if (!draftResult.Succeeded)
        {
            return ValidationFailed(draftResult.Message);
        }

        var updated = await repository.UpdateAsync(id, draftResult.Draft!, cancellationToken);

        if (updated is null)
        {
            return new MedicalServiceResult(
                false,
                "Medical service was not found.",
                FailureReason: MedicalServiceFailureReason.NotFound);
        }

        return new MedicalServiceResult(
            true,
            "Medical service updated successfully.",
            ToResponse(updated));
    }

    public async Task<MedicalServiceResult> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return new MedicalServiceResult(
                false,
                "Medical service id is required.",
                FailureReason: MedicalServiceFailureReason.Validation);
        }

        var deleted = await repository.SoftDeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            return new MedicalServiceResult(
                false,
                "Medical service was not found.",
                FailureReason: MedicalServiceFailureReason.NotFound);
        }

        return new MedicalServiceResult(true, "Medical service deleted successfully.");
    }

    private static DraftResult BuildDraft(UpsertMedicalServiceCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return DraftResult.Failed("Name is required.");
        }

        if (command.DurationMinutes <= 0)
        {
            return DraftResult.Failed("Duration minutes must be greater than 0.");
        }

        if (command.Price < 0)
        {
            return DraftResult.Failed("Price must be greater than or equal to 0.");
        }

        if (command.NoShowFeeAmount < 0)
        {
            return DraftResult.Failed("No-show fee amount must be null or greater than or equal to 0.");
        }

        var currency = string.IsNullOrWhiteSpace(command.Currency)
            ? DefaultCurrency
            : command.Currency.Trim().ToUpperInvariant();

        var draft = new MedicalServiceDraft(
            command.Name.Trim(),
            NormalizeOptional(command.Description),
            command.DurationMinutes,
            command.Price,
            currency,
            command.IsActive,
            command.NoShowFeeEnabled,
            command.NoShowFeeAmount);

        return DraftResult.Valid(draft);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static MedicalServiceResponse ToResponse(MedicalServiceRecord record)
    {
        return new MedicalServiceResponse(
            record.Id,
            record.Name,
            record.Description,
            record.DurationMinutes,
            record.Price,
            record.Currency,
            record.IsActive,
            record.NoShowFeeEnabled,
            record.NoShowFeeAmount);
    }

    private static MedicalServiceResult ValidationFailed(string message)
    {
        return new MedicalServiceResult(
            false,
            message,
            FailureReason: MedicalServiceFailureReason.Validation);
    }

    private sealed record DraftResult(bool Succeeded, string Message, MedicalServiceDraft? Draft)
    {
        public static DraftResult Valid(MedicalServiceDraft draft)
        {
            return new DraftResult(true, string.Empty, draft);
        }

        public static DraftResult Failed(string message)
        {
            return new DraftResult(false, message, null);
        }
    }
}
