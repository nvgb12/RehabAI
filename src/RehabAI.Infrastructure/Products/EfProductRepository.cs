using Microsoft.EntityFrameworkCore;
using RehabAI.Application.Products;
using RehabAI.Domain.Entities;
using RehabAI.Infrastructure.Database;

namespace RehabAI.Infrastructure.Products;

public sealed class EfProductRepository(AppDbContext dbContext) : IProductRepository
{
    public async Task<IReadOnlyList<ProductRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await dbContext.Products
            .Where(product => !product.IsDeleted)
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);

        var categoryNames = await GetCategoryNamesAsync(
            products.Select(product => product.CategoryId).Distinct().ToList(),
            cancellationToken);

        return products.Select(product => ToRecord(product, categoryNames)).ToList();
    }

    public async Task<ProductRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .SingleOrDefaultAsync(product => product.Id == id && !product.IsDeleted, cancellationToken);

        if (product is null)
        {
            return null;
        }

        var categoryNames = await GetCategoryNamesAsync([product.CategoryId], cancellationToken);

        return ToRecord(product, categoryNames);
    }

    public async Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ProductCategories
            .AnyAsync(category => category.Id == categoryId && !category.IsDeleted, cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(
        string slug,
        Guid? excludeProductId = null,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Products
            .AnyAsync(
                product => product.Slug == slug && (!excludeProductId.HasValue || product.Id != excludeProductId.Value),
                cancellationToken);
    }

    public async Task<ProductRecord> CreateAsync(ProductDraft draft, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var product = new Product
        {
            CategoryId = draft.CategoryId,
            Name = draft.Name,
            Slug = draft.Slug,
            Description = draft.Description,
            Price = draft.Price,
            Currency = draft.Currency,
            StockQuantity = draft.StockQuantity,
            ImageUrl = draft.ImageUrl,
            IsActive = draft.IsActive,
            CreatedAt = now
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        var categoryNames = await GetCategoryNamesAsync([product.CategoryId], cancellationToken);

        return ToRecord(product, categoryNames);
    }

    public async Task<ProductRecord?> UpdateAsync(
        Guid id,
        ProductDraft draft,
        CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .SingleOrDefaultAsync(product => product.Id == id && !product.IsDeleted, cancellationToken);

        if (product is null)
        {
            return null;
        }

        product.CategoryId = draft.CategoryId;
        product.Name = draft.Name;
        product.Slug = draft.Slug;
        product.Description = draft.Description;
        product.Price = draft.Price;
        product.Currency = draft.Currency;
        product.StockQuantity = draft.StockQuantity;
        product.ImageUrl = draft.ImageUrl;
        product.IsActive = draft.IsActive;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var categoryNames = await GetCategoryNamesAsync([product.CategoryId], cancellationToken);

        return ToRecord(product, categoryNames);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .SingleOrDefaultAsync(product => product.Id == id && !product.IsDeleted, cancellationToken);

        if (product is null)
        {
            return false;
        }

        product.IsActive = false;
        product.IsDeleted = true;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<Dictionary<Guid, string>> GetCategoryNamesAsync(
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken cancellationToken)
    {
        return await dbContext.ProductCategories
            .Where(category => categoryIds.Contains(category.Id))
            .ToDictionaryAsync(category => category.Id, category => category.Name, cancellationToken);
    }

    private static ProductRecord ToRecord(Product product, IReadOnlyDictionary<Guid, string> categoryNames)
    {
        return new ProductRecord(
            product.Id,
            product.CategoryId,
            categoryNames.GetValueOrDefault(product.CategoryId, string.Empty),
            product.Name,
            product.Slug,
            product.Description,
            product.Price,
            product.Currency,
            product.StockQuantity,
            product.ImageUrl,
            product.IsActive);
    }
}
