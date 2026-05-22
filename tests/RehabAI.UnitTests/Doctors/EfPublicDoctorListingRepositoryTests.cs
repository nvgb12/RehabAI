using Microsoft.EntityFrameworkCore;
using RehabAI.Application.Doctors;
using RehabAI.Domain.Entities;
using RehabAI.Domain.Enums;
using RehabAI.Infrastructure.Database;
using RehabAI.Infrastructure.Doctors;

namespace RehabAI.UnitTests.Doctors;

public class EfPublicDoctorListingRepositoryTests
{
    [Fact]
    public async Task SearchAsync_ReturnsApprovedActiveDoctorEvenWhenNoAvailableSlotExists()
    {
        await using var dbContext = CreateDbContext();
        var doctorProfileId = await SeedDoctorAsync(
            dbContext,
            AccountStatus.Active,
            DoctorProfileReviewStatus.Approved,
            publicProfileApproved: true,
            includeAvailableSlot: false);
        var repository = new EfPublicDoctorListingRepository(dbContext);

        var doctors = await repository.SearchAsync(
            new PublicDoctorSearchCriteria(null, null, null, null),
            DateTimeOffset.UtcNow);

        var doctor = Assert.Single(doctors);
        Assert.Equal(doctorProfileId, doctor.DoctorProfileId);
        Assert.Null(doctor.NextAvailableSlotStartTime);
        Assert.Null(doctor.NextAvailableSlotEndTime);
    }

    [Fact]
    public async Task SearchAsync_ExcludesSubmittedDoctorProfile()
    {
        await using var dbContext = CreateDbContext();
        await SeedDoctorAsync(
            dbContext,
            AccountStatus.Active,
            DoctorProfileReviewStatus.Submitted,
            publicProfileApproved: false,
            includeAvailableSlot: true);
        var repository = new EfPublicDoctorListingRepository(dbContext);

        var doctors = await repository.SearchAsync(
            new PublicDoctorSearchCriteria(null, null, null, null),
            DateTimeOffset.UtcNow);

        Assert.Empty(doctors);
    }

    [Fact]
    public async Task SearchAsync_ReturnsApprovedActiveDoctorWithFutureAvailableSlotMetadata()
    {
        await using var dbContext = CreateDbContext();
        var doctorProfileId = await SeedDoctorAsync(
            dbContext,
            AccountStatus.Active,
            DoctorProfileReviewStatus.Approved,
            publicProfileApproved: true,
            includeAvailableSlot: true);
        var repository = new EfPublicDoctorListingRepository(dbContext);

        var doctors = await repository.SearchAsync(
            new PublicDoctorSearchCriteria(null, null, null, null),
            DateTimeOffset.UtcNow);

        var doctor = Assert.Single(doctors);
        Assert.Equal(doctorProfileId, doctor.DoctorProfileId);
        Assert.NotNull(doctor.NextAvailableSlotStartTime);
        Assert.NotNull(doctor.NextAvailableSlotEndTime);
    }

    [Fact]
    public async Task SearchAsync_WhenAvailabilityFilterIsProvided_DoesNotHideApprovedDoctorWithoutSlot()
    {
        await using var dbContext = CreateDbContext();
        var doctorProfileId = await SeedDoctorAsync(
            dbContext,
            AccountStatus.Active,
            DoctorProfileReviewStatus.Approved,
            publicProfileApproved: true,
            includeAvailableSlot: false);
        var repository = new EfPublicDoctorListingRepository(dbContext);
        var from = DateTimeOffset.UtcNow.AddDays(1);

        var doctors = await repository.SearchAsync(
            new PublicDoctorSearchCriteria(null, null, from, from.AddDays(1)),
            DateTimeOffset.UtcNow);

        var doctor = Assert.Single(doctors);
        Assert.Equal(doctorProfileId, doctor.DoctorProfileId);
        Assert.Null(doctor.NextAvailableSlotStartTime);
        Assert.Null(doctor.NextAvailableSlotEndTime);
    }

    [Fact]
    public async Task SearchAsync_ExcludesRejectedDoctorProfile()
    {
        await using var dbContext = CreateDbContext();
        await SeedDoctorAsync(
            dbContext,
            AccountStatus.Active,
            DoctorProfileReviewStatus.Rejected,
            publicProfileApproved: false,
            includeAvailableSlot: true);
        var repository = new EfPublicDoctorListingRepository(dbContext);

        var doctors = await repository.SearchAsync(
            new PublicDoctorSearchCriteria(null, null, null, null),
            DateTimeOffset.UtcNow);

        Assert.Empty(doctors);
    }

    [Fact]
    public async Task SearchAsync_ExcludesInactiveDoctorUser()
    {
        await using var dbContext = CreateDbContext();
        await SeedDoctorAsync(
            dbContext,
            AccountStatus.Suspended,
            DoctorProfileReviewStatus.Approved,
            publicProfileApproved: true,
            includeAvailableSlot: true);
        var repository = new EfPublicDoctorListingRepository(dbContext);

        var doctors = await repository.SearchAsync(
            new PublicDoctorSearchCriteria(null, null, null, null),
            DateTimeOffset.UtcNow);

        Assert.Empty(doctors);
    }

    [Fact]
    public async Task SearchAsync_ExcludesDeletedDoctorProfile()
    {
        await using var dbContext = CreateDbContext();
        await SeedDoctorAsync(
            dbContext,
            AccountStatus.Active,
            DoctorProfileReviewStatus.Approved,
            publicProfileApproved: true,
            includeAvailableSlot: true,
            isDeleted: true);
        var repository = new EfPublicDoctorListingRepository(dbContext);

        var doctors = await repository.SearchAsync(
            new PublicDoctorSearchCriteria(null, null, null, null),
            DateTimeOffset.UtcNow);

        Assert.Empty(doctors);
    }

    [Fact]
    public async Task SearchAsync_AppliesKeywordAndSpecialtyFilters()
    {
        await using var dbContext = CreateDbContext();
        var doctorProfileId = await SeedDoctorAsync(
            dbContext,
            AccountStatus.Active,
            DoctorProfileReviewStatus.Approved,
            publicProfileApproved: true,
            includeAvailableSlot: false);
        var specialtyId = await dbContext.DoctorProfiles
            .Where(profile => profile.Id == doctorProfileId)
            .Select(profile => profile.SpecialtyId)
            .SingleAsync();
        var repository = new EfPublicDoctorListingRepository(dbContext);

        var doctors = await repository.SearchAsync(
            new PublicDoctorSearchCriteria("Stroke Rehab", specialtyId, null, null),
            DateTimeOffset.UtcNow);

        var doctor = Assert.Single(doctors);
        Assert.Equal(doctorProfileId, doctor.DoctorProfileId);
    }

    private static async Task<Guid> SeedDoctorAsync(
        AppDbContext dbContext,
        AccountStatus accountStatus,
        DoctorProfileReviewStatus reviewStatus,
        bool publicProfileApproved,
        bool includeAvailableSlot,
        bool isDeleted = false)
    {
        var role = new Role
        {
            Name = "Doctor"
        };
        var specialty = new Specialty
        {
            Name = "Stroke Rehabilitation",
            Slug = $"stroke-rehabilitation-{Guid.NewGuid():N}",
            IsActive = true
        };
        var user = new User
        {
            FullName = $"Dr Stroke Rehab {Guid.NewGuid():N}",
            Email = $"doctor-{Guid.NewGuid():N}@test.com",
            PhoneNumber = "0912345678",
            Status = accountStatus,
            EmailConfirmed = true,
            PasswordHash = "hashed"
        };

        dbContext.AddRange(role, specialty, user);
        await dbContext.SaveChangesAsync();

        dbContext.UserRoles.Add(new UserRoleAssignment
        {
            UserId = user.Id,
            RoleId = role.Id
        });
        var profile = new DoctorProfile
        {
            UserId = user.Id,
            SpecialtyId = specialty.Id,
            Bio = "Post-stroke rehabilitation specialist.",
            AvatarUrl = "/uploads/doctor-avatars/doctor.png",
            PublicProfileApproved = publicProfileApproved,
            PublicProfileReviewStatus = reviewStatus,
            IsDeleted = isDeleted
        };
        dbContext.DoctorProfiles.Add(profile);
        await dbContext.SaveChangesAsync();

        if (includeAvailableSlot)
        {
            dbContext.DoctorScheduleSlots.Add(new DoctorScheduleSlot
            {
                DoctorProfileId = profile.Id,
                StartTime = DateTimeOffset.UtcNow.AddDays(1),
                EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
                Status = ScheduleSlotStatus.Available
            });
            await dbContext.SaveChangesAsync();
        }

        return profile.Id;
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
