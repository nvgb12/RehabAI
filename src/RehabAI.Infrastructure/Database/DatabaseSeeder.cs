using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RehabAI.Application.Auth;
using RehabAI.Domain.Entities;
using RehabAI.Domain.Enums;

namespace RehabAI.Infrastructure.Database;

public static class DatabaseSeeder
{
    private static readonly DateTimeOffset SeedTimestamp = new(2026, 5, 14, 0, 0, 0, TimeSpan.Zero);

    public static async Task SeedDatabaseAsync(
        this IServiceProvider serviceProvider,
        bool seedDevelopmentData = false,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await SeedRolesAsync(dbContext, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await SeedSpecialtiesAsync(dbContext, cancellationToken);
        await SeedSubscriptionPlansAsync(dbContext, cancellationToken);
        await SeedSystemSettingsAsync(dbContext, cancellationToken);
        await SeedDevelopmentAdminAsync(
            dbContext,
            configuration,
            seedDevelopmentData,
            passwordHasher,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedRolesAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var roles = new[]
        {
            new RoleSeed("Patient", "Patient account for appointment booking, product orders, subscriptions, and AI chat."),
            new RoleSeed("Doctor", "Admin-created doctor account for consultation schedules and appointments."),
            new RoleSeed("Admin", "Administrator account for platform management."),
            new RoleSeed("AuthorizedInternalStaff", "Delegated internal staff role for approved administrative operations."),
            new RoleSeed("VerificationAdmin", "Internal verification role for doctor credential review."),
            new RoleSeed("SupportStaff", "Support staff role for patient, appointment, and order support."),
            new RoleSeed("FinanceAdmin", "Finance staff role for payments, refunds, and subscription support.")
        };

        foreach (var seed in roles)
        {
            var exists = await dbContext.Roles.AnyAsync(role => role.Name == seed.Name, cancellationToken);

            if (exists)
            {
                continue;
            }

            dbContext.Roles.Add(new Role
            {
                Name = seed.Name,
                Description = seed.Description,
                CreatedAt = SeedTimestamp
            });
        }
    }

    private static async Task SeedSpecialtiesAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var specialties = new[]
        {
            new SpecialtySeed("Physical Therapy", "physical-therapy", "Rehabilitation focused on movement, strength, mobility, and recovery."),
            new SpecialtySeed("Sports Rehabilitation", "sports-rehabilitation", "Recovery and conditioning support for sports-related injuries."),
            new SpecialtySeed("Neurological Rehabilitation", "neurological-rehabilitation", "Rehabilitation support for neurological conditions and functional recovery."),
            new SpecialtySeed("Orthopedic Rehabilitation", "orthopedic-rehabilitation", "Rehabilitation for bones, joints, muscles, and post-operative recovery."),
            new SpecialtySeed("Pain Management", "pain-management", "Support for chronic pain reduction and functional improvement."),
            new SpecialtySeed("Posture And Ergonomics", "posture-and-ergonomics", "Assessment and guidance for posture, work habits, and musculoskeletal strain.")
        };

        foreach (var seed in specialties)
        {
            var exists = await dbContext.Specialties.AnyAsync(specialty => specialty.Slug == seed.Slug, cancellationToken);

            if (exists)
            {
                continue;
            }

            dbContext.Specialties.Add(new Specialty
            {
                Name = seed.Name,
                Slug = seed.Slug,
                Description = seed.Description,
                IsActive = true,
                CreatedAt = SeedTimestamp
            });
        }
    }

    private static async Task SeedSubscriptionPlansAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var plans = new[]
        {
            new SubscriptionPlanSeed("Free", "Free", 0m, 10, 7),
            new SubscriptionPlanSeed("Pro", "Pro", 99000m, 100, 90)
        };

        foreach (var seed in plans)
        {
            var exists = await dbContext.SubscriptionPlans.AnyAsync(plan => plan.Code == seed.Code, cancellationToken);

            if (exists)
            {
                continue;
            }

            dbContext.SubscriptionPlans.Add(new SubscriptionPlan
            {
                Code = seed.Code,
                Name = seed.Name,
                Price = seed.Price,
                DailyMessageLimit = seed.DailyMessageLimit,
                HistoryRetentionDays = seed.HistoryRetentionDays,
                IsActive = true,
                CreatedAt = SeedTimestamp
            });
        }
    }

    private static async Task SeedSystemSettingsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var settings = new[]
        {
            new SystemSettingSeed("Appointment.SoftReserveMinutes", "10", "int", "Minutes a paid appointment slot remains soft-reserved while waiting for payment."),
            new SystemSettingSeed("Appointment.CancellationDeadlineHours", "24", "int", "Minimum hours before appointment start time for standard patient cancellation."),
            new SystemSettingSeed("Platform.DefaultCommissionRate", "15", "decimal", "Default platform commission rate percentage for doctor earnings calculations."),
            new SystemSettingSeed("Payment.WebhookRetryMaxAttempts", "5", "int", "Maximum retry attempts for payment webhook processing.")
        };

        foreach (var seed in settings)
        {
            var exists = await dbContext.SystemSettings.AnyAsync(setting => setting.SettingKey == seed.Key, cancellationToken);

            if (exists)
            {
                continue;
            }

            dbContext.SystemSettings.Add(new SystemSetting
            {
                SettingKey = seed.Key,
                SettingValue = seed.Value,
                ValueType = seed.ValueType,
                Description = seed.Description,
                CreatedAt = SeedTimestamp
            });
        }
    }

    private static async Task SeedDevelopmentAdminAsync(
        AppDbContext dbContext,
        IConfiguration configuration,
        bool seedDevelopmentData,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken)
    {
        if (!seedDevelopmentData)
        {
            return;
        }

        var email = configuration["DevelopmentTestAccounts:Admin:Email"]?.Trim();
        var password = configuration["DevelopmentTestAccounts:Admin:Password"];
        var roleName = configuration["DevelopmentTestAccounts:Admin:Role"]?.Trim();

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(roleName))
        {
            return;
        }

        var role = await dbContext.Roles
            .FirstOrDefaultAsync(existingRole => existingRole.Name == roleName, cancellationToken);

        if (role is null)
        {
            role = new Role
            {
                Name = roleName,
                Description = "Development administrator account role.",
                CreatedAt = SeedTimestamp
            };

            dbContext.Roles.Add(role);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else if (role.IsDeleted)
        {
            role.IsDeleted = false;
            role.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var user = await dbContext.Users
            .Include(existingUser => existingUser.Roles)
            .FirstOrDefaultAsync(existingUser => existingUser.Email == email, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                FullName = "Admin",
                Email = email,
                PasswordHash = passwordHasher.HashPassword(password),
                Status = AccountStatus.Active,
                EmailConfirmed = true,
                IsDeleted = false,
                CreatedAt = SeedTimestamp
            };

            dbContext.Users.Add(user);
        }
        else
        {
            user.FullName = string.IsNullOrWhiteSpace(user.FullName) ? "Admin" : user.FullName;
            user.Status = AccountStatus.Active;
            user.EmailConfirmed = true;
            user.IsDeleted = false;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            if (string.IsNullOrWhiteSpace(user.PasswordHash) ||
                !passwordHasher.VerifyPassword(password, user.PasswordHash))
            {
                user.PasswordHash = passwordHasher.HashPassword(password);
            }
        }

        var hasRole = user.Roles.Any(assignment => assignment.RoleId == role.Id);

        if (!hasRole)
        {
            user.Roles.Add(new UserRoleAssignment
            {
                UserId = user.Id,
                User = user,
                Role = role,
                RoleId = role.Id
            });
        }
    }

    private sealed record RoleSeed(string Name, string Description);

    private sealed record SpecialtySeed(string Name, string Slug, string Description);

    private sealed record SubscriptionPlanSeed(
        string Code,
        string Name,
        decimal Price,
        int DailyMessageLimit,
        int HistoryRetentionDays);

    private sealed record SystemSettingSeed(string Key, string Value, string ValueType, string Description);
}
