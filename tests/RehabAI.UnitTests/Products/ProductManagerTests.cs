using RehabAI.Application.Products;

namespace RehabAI.UnitTests.Products;

public class ProductManagerTests
{
    [Fact]
    public async Task Create_WithValidCommandAndMissingCurrency_DefaultsCurrencyToVnd()
    {
        var categoryId = Guid.NewGuid();
        var repository = new FakeProductRepository(categoryId);
        var manager = new ProductManager(repository);

        var result = await manager.CreateAsync(new UpsertProductCommand(
            "Stroke Mobility Aid",
            "Support product for stroke mobility training.",
            categoryId,
            450000,
            null,
            10,
            null,
            true));

        Assert.True(result.Succeeded);
        Assert.Equal("VND", result.Product!.Currency);
        Assert.Equal("VND", repository.CreatedDraft!.Currency);
    }

    [Fact]
    public async Task Create_WithValidCommand_StoresActiveState()
    {
        var categoryId = Guid.NewGuid();
        var repository = new FakeProductRepository(categoryId);
        var manager = new ProductManager(repository);

        var result = await manager.CreateAsync(new UpsertProductCommand(
            "Post-Stroke Grip Trainer",
            null,
            categoryId,
            250000,
            "vnd",
            5,
            null,
            true));

        Assert.True(result.Succeeded);
        Assert.True(result.Product!.IsActive);
        Assert.True(repository.CreatedDraft!.IsActive);
    }

    public static TheoryData<decimal, int> InvalidPriceOrStock => new()
    {
        { -1m, 10 },
        { 100000m, -1 }
    };

    [Theory]
    [MemberData(nameof(InvalidPriceOrStock))]
    public async Task Create_WithInvalidPriceOrStock_ReturnsValidationFailure(decimal price, int stockQuantity)
    {
        var categoryId = Guid.NewGuid();
        var manager = new ProductManager(new FakeProductRepository(categoryId));

        var result = await manager.CreateAsync(new UpsertProductCommand(
            "Stroke Recovery Therapy Ball",
            null,
            categoryId,
            price,
            "VND",
            stockQuantity,
            null,
            true));

        Assert.False(result.Succeeded);
        Assert.Equal(ProductFailureReason.Validation, result.FailureReason);
    }

    [Fact]
    public async Task Create_WhenCategoryDoesNotExist_ReturnsCategoryNotFound()
    {
        var manager = new ProductManager(new FakeProductRepository());

        var result = await manager.CreateAsync(new UpsertProductCommand(
            "Stroke Recovery Therapy Ball",
            null,
            Guid.NewGuid(),
            100000,
            "VND",
            10,
            null,
            true));

        Assert.False(result.Succeeded);
        Assert.Equal(ProductFailureReason.CategoryNotFound, result.FailureReason);
    }

    [Fact]
    public async Task Create_WhenSlugAlreadyExists_ReturnsConflictReason()
    {
        var categoryId = Guid.NewGuid();
        var repository = new FakeProductRepository(categoryId);
        var manager = new ProductManager(repository);

        await manager.CreateAsync(new UpsertProductCommand(
            "Stroke Recovery Therapy Ball",
            null,
            categoryId,
            100000,
            "VND",
            10,
            null,
            true));

        var result = await manager.CreateAsync(new UpsertProductCommand(
            "Stroke Recovery Therapy Ball",
            null,
            categoryId,
            120000,
            "VND",
            8,
            null,
            true));

        Assert.False(result.Succeeded);
        Assert.Equal(ProductFailureReason.DuplicateSlug, result.FailureReason);
    }

    [Fact]
    public async Task Update_WhenProductDoesNotExist_ReturnsNotFound()
    {
        var categoryId = Guid.NewGuid();
        var manager = new ProductManager(new FakeProductRepository(categoryId));

        var result = await manager.UpdateAsync(Guid.NewGuid(), ValidCommand(categoryId));

        Assert.False(result.Succeeded);
        Assert.Equal(ProductFailureReason.NotFound, result.FailureReason);
    }

    [Fact]
    public async Task SoftDelete_WhenProductExists_ReturnsSuccess()
    {
        var categoryId = Guid.NewGuid();
        var repository = new FakeProductRepository(categoryId);
        var product = await repository.CreateAsync(new ProductDraft(
            categoryId,
            "Stroke Mobility Aid",
            "stroke-mobility-aid",
            null,
            450000,
            "VND",
            10,
            null,
            true));
        var manager = new ProductManager(repository);

        var result = await manager.SoftDeleteAsync(product.Id);

        Assert.True(result.Succeeded);
        Assert.Contains(product.Id, repository.DeletedIds);
    }

    [Fact]
    public async Task GetPublicProducts_ReturnsOnlyActiveInStockNonDeletedProducts()
    {
        var categoryId = Guid.NewGuid();
        var repository = new FakeProductRepository(categoryId);
        var active = await repository.CreateAsync(ProductDraft(
            categoryId,
            "Stroke Mobility Aid",
            "stroke-mobility-aid",
            isActive: true,
            stockQuantity: 10));
        await repository.CreateAsync(ProductDraft(
            categoryId,
            "Inactive Stroke Grip Trainer",
            "inactive-stroke-grip-trainer",
            isActive: false,
            stockQuantity: 10));
        await repository.CreateAsync(ProductDraft(
            categoryId,
            "Out of Stock Therapy Ball",
            "out-of-stock-therapy-ball",
            isActive: true,
            stockQuantity: 0));
        var deleted = await repository.CreateAsync(ProductDraft(
            categoryId,
            "Deleted Stroke Walker",
            "deleted-stroke-walker",
            isActive: true,
            stockQuantity: 5));
        await repository.SoftDeleteAsync(deleted.Id);
        var manager = new ProductManager(repository);

        var products = await manager.GetPublicProductsAsync(new PublicProductQuery(null, null));

        var product = Assert.Single(products);
        Assert.Equal(active.Id, product.ProductId);
    }

    [Fact]
    public async Task GetPublicProductById_WhenProductIsActiveAndInStock_ReturnsProduct()
    {
        var categoryId = Guid.NewGuid();
        var repository = new FakeProductRepository(categoryId);
        var product = await repository.CreateAsync(ProductDraft(
            categoryId,
            "Stroke Mobility Aid",
            "stroke-mobility-aid",
            isActive: true,
            stockQuantity: 10));
        var manager = new ProductManager(repository);

        var result = await manager.GetPublicProductByIdAsync(product.Id);

        Assert.NotNull(result);
        Assert.Equal(product.Id, result!.ProductId);
    }

    [Fact]
    public async Task GetPublicProductById_WhenProductIsInactive_ReturnsNull()
    {
        var categoryId = Guid.NewGuid();
        var repository = new FakeProductRepository(categoryId);
        var product = await repository.CreateAsync(ProductDraft(
            categoryId,
            "Inactive Stroke Grip Trainer",
            "inactive-stroke-grip-trainer",
            isActive: false,
            stockQuantity: 10));
        var manager = new ProductManager(repository);

        var result = await manager.GetPublicProductByIdAsync(product.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPublicProductById_WhenProductIsDeleted_ReturnsNull()
    {
        var categoryId = Guid.NewGuid();
        var repository = new FakeProductRepository(categoryId);
        var product = await repository.CreateAsync(ProductDraft(
            categoryId,
            "Deleted Stroke Walker",
            "deleted-stroke-walker",
            isActive: true,
            stockQuantity: 10));
        await repository.SoftDeleteAsync(product.Id);
        var manager = new ProductManager(repository);

        var result = await manager.GetPublicProductByIdAsync(product.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPublicProducts_WithKeywordAndCategory_FiltersProducts()
    {
        var matchingCategoryId = Guid.NewGuid();
        var otherCategoryId = Guid.NewGuid();
        var repository = new FakeProductRepository(matchingCategoryId, otherCategoryId);
        var matchingProduct = await repository.CreateAsync(ProductDraft(
            matchingCategoryId,
            "Post-Stroke Grip Trainer",
            "post-stroke-grip-trainer",
            isActive: true,
            stockQuantity: 10));
        await repository.CreateAsync(ProductDraft(
            matchingCategoryId,
            "Stroke Mobility Aid",
            "stroke-mobility-aid",
            isActive: true,
            stockQuantity: 10));
        await repository.CreateAsync(ProductDraft(
            otherCategoryId,
            "Post-Stroke Walking Cane",
            "post-stroke-walking-cane",
            isActive: true,
            stockQuantity: 10));
        var manager = new ProductManager(repository);

        var products = await manager.GetPublicProductsAsync(new PublicProductQuery("grip", matchingCategoryId));

        var product = Assert.Single(products);
        Assert.Equal(matchingProduct.Id, product.ProductId);
    }

    private static UpsertProductCommand ValidCommand(Guid categoryId)
    {
        return new UpsertProductCommand(
            "Stroke Mobility Aid",
            "Support product for stroke mobility training.",
            categoryId,
            450000,
            "VND",
            10,
            null,
            true);
    }

    private static ProductDraft ProductDraft(
        Guid categoryId,
        string name,
        string slug,
        bool isActive,
        int stockQuantity)
    {
        return new ProductDraft(
            categoryId,
            name,
            slug,
            "Support product for stroke rehabilitation.",
            100000,
            "VND",
            stockQuantity,
            null,
            isActive);
    }

    private sealed class FakeProductRepository(params Guid[] categoryIds) : IProductRepository
    {
        private readonly HashSet<Guid> existingCategoryIds = [.. categoryIds];
        private readonly Dictionary<Guid, ProductRecord> products = [];
        private readonly HashSet<Guid> deletedProductIds = [];

        public ProductDraft? CreatedDraft { get; private set; }
        public List<Guid> DeletedIds { get; } = [];

        public Task<IReadOnlyList<ProductRecord>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                products.Values.Where(product => !deletedProductIds.Contains(product.Id)).ToList() as IReadOnlyList<ProductRecord>);
        }

        public Task<ProductRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (deletedProductIds.Contains(id))
            {
                return Task.FromResult<ProductRecord?>(null);
            }

            products.TryGetValue(id, out var product);

            return Task.FromResult(product);
        }

        public Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(existingCategoryIds.Contains(categoryId));
        }

        public Task<bool> SlugExistsAsync(
            string slug,
            Guid? excludeProductId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(products.Values.Any(
                product => product.Slug == slug && (!excludeProductId.HasValue || product.Id != excludeProductId.Value)));
        }

        public Task<ProductRecord> CreateAsync(ProductDraft draft, CancellationToken cancellationToken = default)
        {
            CreatedDraft = draft;
            var record = ToRecord(Guid.NewGuid(), draft);
            products[record.Id] = record;

            return Task.FromResult(record);
        }

        public Task<ProductRecord?> UpdateAsync(
            Guid id,
            ProductDraft draft,
            CancellationToken cancellationToken = default)
        {
            if (!products.ContainsKey(id))
            {
                return Task.FromResult<ProductRecord?>(null);
            }

            var record = ToRecord(id, draft);
            products[id] = record;

            return Task.FromResult<ProductRecord?>(record);
        }

        public Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (!products.ContainsKey(id) || deletedProductIds.Contains(id))
            {
                return Task.FromResult(false);
            }

            DeletedIds.Add(id);
            deletedProductIds.Add(id);

            return Task.FromResult(true);
        }

        private static ProductRecord ToRecord(Guid id, ProductDraft draft)
        {
            return new ProductRecord(
                id,
                draft.CategoryId,
                "Stroke Rehabilitation Equipment",
                draft.Name,
                draft.Slug,
                draft.Description,
                draft.Price,
                draft.Currency,
                draft.StockQuantity,
                draft.ImageUrl,
                draft.IsActive);
        }
    }
}
