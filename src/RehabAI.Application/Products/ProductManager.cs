using System.Text;

namespace RehabAI.Application.Products;

public sealed class ProductManager(IProductRepository repository) : IProductManager
{
    private const string DefaultCurrency = "VND";

    public async Task<IReadOnlyList<ProductResponse>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await repository.GetAllAsync(cancellationToken);

        return products.Select(ToResponse).ToList();
    }

    public async Task<ProductResponse?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await repository.GetByIdAsync(id, cancellationToken);

        return product is null ? null : ToResponse(product);
    }

    public async Task<IReadOnlyList<PublicProductResponse>> GetPublicProductsAsync(
        PublicProductQuery query,
        CancellationToken cancellationToken = default)
    {
        var products = await repository.GetAllAsync(cancellationToken);
        var filteredProducts = products
            .Where(IsPubliclyVisible)
            .Where(product => MatchesQuery(product, query))
            .Select(ToPublicResponse)
            .ToList();

        return filteredProducts;
    }

    public async Task<PublicProductResponse?> GetPublicProductByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        var product = await repository.GetByIdAsync(id, cancellationToken);

        return product is not null && IsPubliclyVisible(product)
            ? ToPublicResponse(product)
            : null;
    }

    public async Task<ProductResult> CreateAsync(
        UpsertProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var draftResult = await BuildDraftAsync(command, null, cancellationToken);

        if (!draftResult.Succeeded)
        {
            return draftResult.ToFailureResult();
        }

        var created = await repository.CreateAsync(draftResult.Draft!, cancellationToken);

        return new ProductResult(
            true,
            "Product created successfully.",
            ToResponse(created));
    }

    public async Task<ProductResult> UpdateAsync(
        Guid id,
        UpsertProductCommand command,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return ValidationFailed("Product id is required.");
        }

        var draftResult = await BuildDraftAsync(command, id, cancellationToken);

        if (!draftResult.Succeeded)
        {
            return draftResult.ToFailureResult();
        }

        var updated = await repository.UpdateAsync(id, draftResult.Draft!, cancellationToken);

        if (updated is null)
        {
            return new ProductResult(
                false,
                "Product was not found.",
                FailureReason: ProductFailureReason.NotFound);
        }

        return new ProductResult(
            true,
            "Product updated successfully.",
            ToResponse(updated));
    }

    public async Task<ProductResult> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return ValidationFailed("Product id is required.");
        }

        var deleted = await repository.SoftDeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            return new ProductResult(
                false,
                "Product was not found.",
                FailureReason: ProductFailureReason.NotFound);
        }

        return new ProductResult(true, "Product deleted successfully.");
    }

    private async Task<DraftResult> BuildDraftAsync(
        UpsertProductCommand command,
        Guid? existingProductId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return DraftResult.Failed("Name is required.", ProductFailureReason.Validation);
        }

        if (command.CategoryId == Guid.Empty)
        {
            return DraftResult.Failed("Category id is required.", ProductFailureReason.Validation);
        }

        if (command.Price < 0)
        {
            return DraftResult.Failed("Price must be greater than or equal to 0.", ProductFailureReason.Validation);
        }

        if (command.StockQuantity < 0)
        {
            return DraftResult.Failed("Stock quantity must be greater than or equal to 0.", ProductFailureReason.Validation);
        }

        var categoryExists = await repository.CategoryExistsAsync(command.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            return DraftResult.Failed("Product category was not found.", ProductFailureReason.CategoryNotFound);
        }

        var name = command.Name.Trim();
        var slug = CreateSlug(name);

        if (string.IsNullOrWhiteSpace(slug))
        {
            return DraftResult.Failed("Product slug could not be generated from the name.", ProductFailureReason.Validation);
        }

        var slugExists = await repository.SlugExistsAsync(slug, existingProductId, cancellationToken);

        if (slugExists)
        {
            return DraftResult.Failed("A product with the same slug already exists.", ProductFailureReason.DuplicateSlug);
        }

        var currency = string.IsNullOrWhiteSpace(command.Currency)
            ? DefaultCurrency
            : command.Currency.Trim().ToUpperInvariant();

        var draft = new ProductDraft(
            command.CategoryId,
            name,
            slug,
            NormalizeOptional(command.Description),
            command.Price,
            currency,
            command.StockQuantity,
            NormalizeOptional(command.ImageUrl),
            command.IsActive);

        return DraftResult.Valid(draft);
    }

    private static string CreateSlug(string value)
    {
        var builder = new StringBuilder();
        var previousDash = false;

        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousDash = false;
                continue;
            }

            if ((char.IsWhiteSpace(character) || character is '-' or '_') && !previousDash && builder.Length > 0)
            {
                builder.Append('-');
                previousDash = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static ProductResponse ToResponse(ProductRecord record)
    {
        return new ProductResponse(
            record.Id,
            record.CategoryId,
            record.CategoryName,
            record.Name,
            record.Slug,
            record.Description,
            record.Price,
            record.Currency,
            record.StockQuantity,
            record.ImageUrl,
            record.IsActive);
    }

    private static PublicProductResponse ToPublicResponse(ProductRecord record)
    {
        return new PublicProductResponse(
            record.Id,
            record.CategoryId,
            record.CategoryName,
            record.Name,
            record.Slug,
            record.Description,
            record.Price,
            record.Currency,
            record.StockQuantity,
            record.ImageUrl);
    }

    private static bool IsPubliclyVisible(ProductRecord product)
    {
        return product.IsActive && product.StockQuantity > 0;
    }

    private static bool MatchesQuery(ProductRecord product, PublicProductQuery query)
    {
        if (query.CategoryId.HasValue && product.CategoryId != query.CategoryId.Value)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(query.Keyword))
        {
            return true;
        }

        var keyword = query.Keyword.Trim();

        return product.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || product.Slug.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || (product.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static ProductResult ValidationFailed(string message)
    {
        return new ProductResult(
            false,
            message,
            FailureReason: ProductFailureReason.Validation);
    }

    private sealed record DraftResult(
        bool Succeeded,
        string Message,
        ProductDraft? Draft,
        ProductFailureReason? FailureReason)
    {
        public static DraftResult Valid(ProductDraft draft)
        {
            return new DraftResult(true, string.Empty, draft, null);
        }

        public static DraftResult Failed(string message, ProductFailureReason failureReason)
        {
            return new DraftResult(false, message, null, failureReason);
        }

        public ProductResult ToFailureResult()
        {
            return new ProductResult(false, Message, FailureReason: FailureReason);
        }
    }
}
