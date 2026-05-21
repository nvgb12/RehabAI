using Microsoft.EntityFrameworkCore;
using RehabAI.Application.Lookups;
using RehabAI.Infrastructure.Database;

namespace RehabAI.Infrastructure.Lookups;

public sealed class EfLookupRepository(AppDbContext dbContext) : ILookupRepository
{
    public async Task<IReadOnlyList<LookupItemRecord>> GetActiveSpecialtiesAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Specialties
            .AsNoTracking()
            .Where(specialty => specialty.IsActive && !specialty.IsDeleted)
            .OrderBy(specialty => specialty.Name)
            .Select(specialty => new LookupItemRecord(
                specialty.Id,
                specialty.Name,
                specialty.Slug,
                specialty.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LookupItemRecord>> GetProductCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ProductCategories
            .AsNoTracking()
            .Where(category => !category.IsDeleted)
            .OrderBy(category => category.Name)
            .Select(category => new LookupItemRecord(
                category.Id,
                category.Name,
                category.Slug,
                null))
            .ToListAsync(cancellationToken);
    }
}
