using Microsoft.EntityFrameworkCore;
using RehabAI.Domain.Entities;
using RehabAI.Infrastructure.Database;
using RehabAI.Infrastructure.Lookups;

namespace RehabAI.UnitTests.Lookups;

public class EfLookupRepositoryTests
{
    [Fact]
    public async Task GetActiveSpecialtiesAsync_ReturnsOnlyActiveNonDeletedSpecialties()
    {
        await using var dbContext = CreateDbContext();
        var active = new Specialty
        {
            Name = "Stroke Rehabilitation",
            Slug = "stroke-rehabilitation",
            Description = "Post-stroke recovery care.",
            IsActive = true
        };
        dbContext.Specialties.AddRange(
            active,
            new Specialty
            {
                Name = "Inactive Specialty",
                Slug = "inactive-specialty",
                IsActive = false
            },
            new Specialty
            {
                Name = "Deleted Specialty",
                Slug = "deleted-specialty",
                IsActive = true,
                IsDeleted = true
            });
        await dbContext.SaveChangesAsync();
        var repository = new EfLookupRepository(dbContext);

        var specialties = await repository.GetActiveSpecialtiesAsync();

        var specialty = Assert.Single(specialties);
        Assert.Equal(active.Id, specialty.Id);
        Assert.Equal(active.Description, specialty.Description);
    }

    [Fact]
    public async Task GetProductCategoriesAsync_ReturnsOnlyNonDeletedCategories()
    {
        await using var dbContext = CreateDbContext();
        var active = new ProductCategory
        {
            Name = "Stroke Recovery Products",
            Slug = "stroke-recovery-products"
        };
        dbContext.ProductCategories.AddRange(
            active,
            new ProductCategory
            {
                Name = "Deleted Products",
                Slug = "deleted-products",
                IsDeleted = true
            });
        await dbContext.SaveChangesAsync();
        var repository = new EfLookupRepository(dbContext);

        var categories = await repository.GetProductCategoriesAsync();

        var category = Assert.Single(categories);
        Assert.Equal(active.Id, category.Id);
        Assert.Equal(active.Name, category.Name);
        Assert.Null(category.Description);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
