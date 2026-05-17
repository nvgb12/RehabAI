namespace RehabAI.Api.Contracts.MedicalServices;

public sealed record CreateMedicalServiceRequest(
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    string? Currency,
    bool? IsActive,
    bool NoShowFeeEnabled,
    decimal? NoShowFeeAmount);

public sealed record UpsertMedicalServiceRequest(
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    string? Currency,
    bool IsActive,
    bool NoShowFeeEnabled,
    decimal? NoShowFeeAmount);
