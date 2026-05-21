namespace RehabAI.Application.Lookups;

public sealed class LookupService(ILookupRepository repository) : ILookupService
{
    public async Task<IReadOnlyList<LookupItemResponse>> GetSpecialtiesAsync(
        CancellationToken cancellationToken = default)
    {
        var specialties = await repository.GetActiveSpecialtiesAsync(cancellationToken);

        return specialties.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<LookupItemResponse>> GetProductCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = await repository.GetProductCategoriesAsync(cancellationToken);

        return categories.Select(ToResponse).ToList();
    }

    private static LookupItemResponse ToResponse(LookupItemRecord record)
    {
        return new LookupItemResponse(
            record.Id,
            record.Name,
            record.Slug,
            record.Description);
    }
}
