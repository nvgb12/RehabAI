using Microsoft.EntityFrameworkCore;
using RehabAI.Domain.Entities;
using RehabAI.Domain.Enums;
using RehabAI.Infrastructure.Database;
using RehabAI.Infrastructure.Doctors;

namespace RehabAI.UnitTests.Doctors;

public class EfDoctorDashboardRepositoryTests
{
    [Fact]
    public async Task GetAppointmentsByUserIdAsync_ReturnsOnlyCurrentDoctorAppointments()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedDoctorDashboardDataAsync(dbContext, includeCurrentDoctorPayment: true);
        var repository = new EfDoctorDashboardRepository(dbContext);

        var appointments = await repository.GetAppointmentsByUserIdAsync(fixture.DoctorUserId);

        var appointment = Assert.Single(appointments);
        Assert.Equal(fixture.AppointmentId, appointment.AppointmentId);
        Assert.Equal("Stroke Rehab Patient", appointment.PatientName);
        Assert.Equal(PaymentStatus.Paid, appointment.PaymentStatus);
    }

    [Fact]
    public async Task GetAppointmentsByUserIdAsync_WhenAppointmentHasNoPayment_ReturnsNullPaymentStatus()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedDoctorDashboardDataAsync(dbContext, includeCurrentDoctorPayment: false);
        var repository = new EfDoctorDashboardRepository(dbContext);

        var appointments = await repository.GetAppointmentsByUserIdAsync(fixture.DoctorUserId);

        var appointment = Assert.Single(appointments);
        Assert.Equal(fixture.AppointmentId, appointment.AppointmentId);
        Assert.Null(appointment.PaymentStatus);
    }

    [Fact]
    public async Task GetAppointmentByUserIdAsync_WhenAppointmentBelongsToAnotherDoctor_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedDoctorDashboardDataAsync(dbContext, includeCurrentDoctorPayment: true);
        var repository = new EfDoctorDashboardRepository(dbContext);

        var appointment = await repository.GetAppointmentByUserIdAsync(
            fixture.DoctorUserId,
            fixture.OtherDoctorAppointmentId);

        Assert.Null(appointment);
    }

    [Fact]
    public async Task GetDashboardSnapshotAsync_WhenDoctorHasAppointments_ReturnsCountsAndNextAppointment()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedDoctorDashboardDataAsync(dbContext, includeCurrentDoctorPayment: true);
        var repository = new EfDoctorDashboardRepository(dbContext);

        var dashboard = await repository.GetDashboardSnapshotAsync(fixture.DoctorUserId, fixture.Now);

        Assert.NotNull(dashboard);
        Assert.Equal(1, dashboard.UpcomingAppointmentCount);
        Assert.Equal(1, dashboard.TodayAppointmentCount);
        Assert.Equal(1, dashboard.AvailableSlotCount);
        Assert.Equal(1, dashboard.BookedSlotCount);
        Assert.NotNull(dashboard.NextAppointment);
        Assert.Equal(fixture.AppointmentId, dashboard.NextAppointment.AppointmentId);
    }

    [Fact]
    public async Task GetDashboardSnapshotAsync_WhenDoctorHasNoAppointments_ReturnsEmptyDashboard()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedDoctorDashboardDataAsync(dbContext, includeCurrentDoctorPayment: true);
        var emptyDoctorUserId = Guid.NewGuid();
        var specialtyId = fixture.SpecialtyId;
        dbContext.Users.Add(new User
        {
            Id = emptyDoctorUserId,
            FullName = "Dr Empty Schedule",
            Email = "empty-doctor@test.com",
            Status = AccountStatus.Active,
            EmailConfirmed = true,
            PasswordHash = "hashed"
        });
        dbContext.DoctorProfiles.Add(new DoctorProfile
        {
            UserId = emptyDoctorUserId,
            SpecialtyId = specialtyId,
            PublicProfileApproved = true
        });
        await dbContext.SaveChangesAsync();
        var repository = new EfDoctorDashboardRepository(dbContext);

        var dashboard = await repository.GetDashboardSnapshotAsync(emptyDoctorUserId, fixture.Now);

        Assert.NotNull(dashboard);
        Assert.Equal(0, dashboard.UpcomingAppointmentCount);
        Assert.Equal(0, dashboard.TodayAppointmentCount);
        Assert.Equal(0, dashboard.AvailableSlotCount);
        Assert.Equal(0, dashboard.BookedSlotCount);
        Assert.Null(dashboard.NextAppointment);
    }

    private static async Task<DoctorDashboardFixture> SeedDoctorDashboardDataAsync(
        AppDbContext dbContext,
        bool includeCurrentDoctorPayment)
    {
        var now = new DateTimeOffset(2026, 5, 21, 9, 0, 0, TimeSpan.Zero);
        var specialty = new Specialty
        {
            Name = "Stroke Rehabilitation",
            Slug = $"stroke-rehabilitation-{Guid.NewGuid():N}",
            IsActive = true
        };
        var doctorUser = new User
        {
            FullName = "Dr Stroke Rehab",
            Email = $"doctor-{Guid.NewGuid():N}@test.com",
            Status = AccountStatus.Active,
            EmailConfirmed = true,
            PasswordHash = "hashed"
        };
        var otherDoctorUser = new User
        {
            FullName = "Dr Other Rehab",
            Email = $"other-doctor-{Guid.NewGuid():N}@test.com",
            Status = AccountStatus.Active,
            EmailConfirmed = true,
            PasswordHash = "hashed"
        };
        var patientUser = new User
        {
            FullName = "Stroke Rehab Patient",
            Email = $"patient-{Guid.NewGuid():N}@test.com",
            Status = AccountStatus.Active,
            EmailConfirmed = true,
            PasswordHash = "hashed"
        };
        var service = new MedicalService
        {
            Name = "Post-stroke rehabilitation consultation",
            DurationMinutes = 60,
            Price = 300000,
            Currency = "VND",
            IsActive = true
        };

        dbContext.AddRange(specialty, doctorUser, otherDoctorUser, patientUser, service);
        await dbContext.SaveChangesAsync();

        var doctorProfile = new DoctorProfile
        {
            UserId = doctorUser.Id,
            SpecialtyId = specialty.Id,
            PublicProfileApproved = true
        };
        var otherDoctorProfile = new DoctorProfile
        {
            UserId = otherDoctorUser.Id,
            SpecialtyId = specialty.Id,
            PublicProfileApproved = true
        };
        var patientProfile = new PatientProfile
        {
            UserId = patientUser.Id
        };

        dbContext.AddRange(doctorProfile, otherDoctorProfile, patientProfile);
        await dbContext.SaveChangesAsync();

        var availableSlot = new DoctorScheduleSlot
        {
            DoctorProfileId = doctorProfile.Id,
            StartTime = now.AddHours(2),
            EndTime = now.AddHours(3),
            Status = ScheduleSlotStatus.Available
        };
        var bookedSlot = new DoctorScheduleSlot
        {
            DoctorProfileId = doctorProfile.Id,
            StartTime = now.AddHours(4),
            EndTime = now.AddHours(5),
            Status = ScheduleSlotStatus.Booked
        };
        var otherDoctorSlot = new DoctorScheduleSlot
        {
            DoctorProfileId = otherDoctorProfile.Id,
            StartTime = now.AddHours(2),
            EndTime = now.AddHours(3),
            Status = ScheduleSlotStatus.Booked
        };

        dbContext.DoctorScheduleSlots.AddRange(availableSlot, bookedSlot, otherDoctorSlot);
        await dbContext.SaveChangesAsync();

        var appointment = new Appointment
        {
            PatientId = patientUser.Id,
            DoctorProfileId = doctorProfile.Id,
            MedicalServiceId = service.Id,
            DoctorScheduleSlotId = bookedSlot.Id,
            StartTime = now.AddHours(4),
            EndTime = now.AddHours(5),
            Status = AppointmentStatus.Confirmed,
            Notes = "Stroke mobility assessment",
            CreatedAt = now.AddDays(-1)
        };
        var otherDoctorAppointment = new Appointment
        {
            PatientId = patientUser.Id,
            DoctorProfileId = otherDoctorProfile.Id,
            MedicalServiceId = service.Id,
            DoctorScheduleSlotId = otherDoctorSlot.Id,
            StartTime = now.AddHours(4),
            EndTime = now.AddHours(5),
            Status = AppointmentStatus.Confirmed,
            Notes = "Other doctor appointment",
            CreatedAt = now.AddDays(-1)
        };

        dbContext.Appointments.AddRange(appointment, otherDoctorAppointment);
        await dbContext.SaveChangesAsync();

        if (includeCurrentDoctorPayment)
        {
            dbContext.Payments.Add(new Payment
            {
                Purpose = PaymentPurpose.Appointment,
                AppointmentId = appointment.Id,
                Amount = 300000,
                Currency = "VND",
                Status = PaymentStatus.Paid,
                CreatedAt = now.AddMinutes(1)
            });
            await dbContext.SaveChangesAsync();
        }

        return new DoctorDashboardFixture(
            now,
            specialty.Id,
            doctorUser.Id,
            appointment.Id,
            otherDoctorAppointment.Id);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private sealed record DoctorDashboardFixture(
        DateTimeOffset Now,
        Guid SpecialtyId,
        Guid DoctorUserId,
        Guid AppointmentId,
        Guid OtherDoctorAppointmentId);
}
