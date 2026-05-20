namespace RehabAI.Application.Products;

public sealed record ProductResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string Slug,
    string? Description,
    decimal Price,
    string Currency,
    int StockQuantity,
    string? ImageUrl,
    bool IsActive);

public sealed record PublicProductResponse(
    Guid ProductId,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string Slug,
    string? Description,
    decimal Price,
    string Currency,
    int StockQuantity,
    string? ImageUrl);

public sealed record PublicProductQuery(
    string? Keyword,
    Guid? CategoryId);

public sealed record UpsertProductCommand(
    string Name,
    string? Description,
    Guid CategoryId,
    decimal Price,
    string? Currency,
    int StockQuantity,
    string? ImageUrl,
    bool IsActive);

public sealed record ProductResult(
    bool Succeeded,
    string Message,
    ProductResponse? Product = null,
    ProductFailureReason? FailureReason = null);

public enum ProductFailureReason
{
    Validation = 1,
    NotFound = 2,
    CategoryNotFound = 3,
    DuplicateSlug = 4
}

public interface IProductManager
{
    Task<IReadOnlyList<ProductResponse>> GetProductsAsync(CancellationToken cancellationToken = default);
    Task<ProductResponse?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PublicProductResponse>> GetPublicProductsAsync(
        PublicProductQuery query,
        CancellationToken cancellationToken = default);

    Task<PublicProductResponse?> GetPublicProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductResult> CreateAsync(UpsertProductCommand command, CancellationToken cancellationToken = default);
    Task<ProductResult> UpdateAsync(Guid id, UpsertProductCommand command, CancellationToken cancellationToken = default);
    Task<ProductResult> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IProductRepository
{
    Task<IReadOnlyList<ProductRecord>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeProductId = null, CancellationToken cancellationToken = default);
    Task<ProductRecord> CreateAsync(ProductDraft draft, CancellationToken cancellationToken = default);
    Task<ProductRecord?> UpdateAsync(Guid id, ProductDraft draft, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record ProductDraft(
    Guid CategoryId,
    string Name,
    string Slug,
    string? Description,
    decimal Price,
    string Currency,
    int StockQuantity,
    string? ImageUrl,
    bool IsActive);

public sealed record ProductRecord(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string Slug,
    string? Description,
    decimal Price,
    string Currency,
    int StockQuantity,
    string? ImageUrl,
    bool IsActive);
