namespace RehabAI.Api.Contracts.Products;

public sealed record CreateProductRequest(
    string Name,
    string? Description,
    Guid CategoryId,
    decimal Price,
    string? Currency,
    int StockQuantity,
    string? ImageUrl,
    bool? IsActive);

public sealed record UpdateProductRequest(
    string Name,
    string? Description,
    Guid CategoryId,
    decimal Price,
    string? Currency,
    int StockQuantity,
    string? ImageUrl,
    bool IsActive);
