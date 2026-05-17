namespace RehabAI.Application.MedicalServices;

public sealed record MedicalServiceResponse(
    Guid Id,
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    string Currency,
    bool IsActive,
    bool NoShowFeeEnabled,
    decimal? NoShowFeeAmount);

public sealed record UpsertMedicalServiceCommand(
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    string? Currency,
    bool IsActive,
    bool NoShowFeeEnabled,
    decimal? NoShowFeeAmount);

public sealed record MedicalServiceResult(
    bool Succeeded,
    string Message,
    MedicalServiceResponse? MedicalService = null,
    MedicalServiceFailureReason? FailureReason = null);

public enum MedicalServiceFailureReason
{
    Validation = 1,
    NotFound = 2
}

public interface IMedicalServiceManager
{
    Task<IReadOnlyList<MedicalServiceResponse>> GetActiveMedicalServicesAsync(CancellationToken cancellationToken = default);
    Task<MedicalServiceResponse?> GetActiveMedicalServiceByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MedicalServiceResult> CreateAsync(UpsertMedicalServiceCommand command, CancellationToken cancellationToken = default);
    Task<MedicalServiceResult> UpdateAsync(Guid id, UpsertMedicalServiceCommand command, CancellationToken cancellationToken = default);
    Task<MedicalServiceResult> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IMedicalServiceRepository
{
    Task<IReadOnlyList<MedicalServiceRecord>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<MedicalServiceRecord?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MedicalServiceRecord> CreateAsync(MedicalServiceDraft draft, CancellationToken cancellationToken = default);
    Task<MedicalServiceRecord?> UpdateAsync(Guid id, MedicalServiceDraft draft, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record MedicalServiceDraft(
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    string Currency,
    bool IsActive,
    bool NoShowFeeEnabled,
    decimal? NoShowFeeAmount);

public sealed record MedicalServiceRecord(
    Guid Id,
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    string Currency,
    bool IsActive,
    bool NoShowFeeEnabled,
    decimal? NoShowFeeAmount);
