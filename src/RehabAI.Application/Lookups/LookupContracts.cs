namespace RehabAI.Application.Lookups;

public sealed record LookupItemResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description);

public interface ILookupService
{
    Task<IReadOnlyList<LookupItemResponse>> GetSpecialtiesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupItemResponse>> GetProductCategoriesAsync(CancellationToken cancellationToken = default);
}

public interface ILookupRepository
{
    Task<IReadOnlyList<LookupItemRecord>> GetActiveSpecialtiesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupItemRecord>> GetProductCategoriesAsync(CancellationToken cancellationToken = default);
}

public sealed record LookupItemRecord(
    Guid Id,
    string Name,
    string Slug,
    string? Description);
