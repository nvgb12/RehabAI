using RehabAI.Application.Lookups;

namespace RehabAI.UnitTests.Lookups;

public class LookupServiceTests
{
    [Fact]
    public async Task GetSpecialtiesAsync_ReturnsRepositorySpecialties()
    {
        var repository = new FakeLookupRepository();
        var service = new LookupService(repository);

        var specialties = await service.GetSpecialtiesAsync();

        var specialty = Assert.Single(specialties);
        Assert.Equal(repository.SpecialtyId, specialty.Id);
        Assert.Equal("Stroke Rehabilitation", specialty.Name);
        Assert.Equal("stroke-rehabilitation", specialty.Slug);
        Assert.Equal("Post-stroke recovery care.", specialty.Description);
    }

    [Fact]
    public async Task GetProductCategoriesAsync_ReturnsRepositoryCategories()
    {
        var repository = new FakeLookupRepository();
        var service = new LookupService(repository);

        var categories = await service.GetProductCategoriesAsync();

        var category = Assert.Single(categories);
        Assert.Equal(repository.CategoryId, category.Id);
        Assert.Equal("Stroke Recovery Products", category.Name);
        Assert.Equal("stroke-recovery-products", category.Slug);
        Assert.Null(category.Description);
    }

    private sealed class FakeLookupRepository : ILookupRepository
    {
        public Guid SpecialtyId { get; } = Guid.NewGuid();
        public Guid CategoryId { get; } = Guid.NewGuid();

        public Task<IReadOnlyList<LookupItemRecord>> GetActiveSpecialtiesAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<LookupItemRecord>>(
                [
                    new LookupItemRecord(
                        SpecialtyId,
                        "Stroke Rehabilitation",
                        "stroke-rehabilitation",
                        "Post-stroke recovery care.")
                ]);
        }

        public Task<IReadOnlyList<LookupItemRecord>> GetProductCategoriesAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<LookupItemRecord>>(
                [
                    new LookupItemRecord(
                        CategoryId,
                        "Stroke Recovery Products",
                        "stroke-recovery-products",
                        null)
                ]);
        }
    }
}
