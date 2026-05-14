using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using RehabAI.Domain.Entities;

namespace RehabAI.Infrastructure.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRoleAssignment> UserRoles => Set<UserRoleAssignment>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();
    public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();
    public DbSet<DoctorProfile> DoctorProfiles => Set<DoctorProfile>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<DoctorCredentialDocument> DoctorCredentialDocuments => Set<DoctorCredentialDocument>();
    public DbSet<MedicalService> MedicalServices => Set<MedicalService>();
    public DbSet<DoctorService> DoctorServices => Set<DoctorService>();
    public DbSet<DoctorScheduleSlot> DoctorScheduleSlots => Set<DoctorScheduleSlot>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<RehabAI.Domain.Entities.Payment> Payments => Set<RehabAI.Domain.Entities.Payment>();
    public DbSet<PaymentWebhookEvent> PaymentWebhookEvents => Set<PaymentWebhookEvent>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Dispute> Disputes => Set<Dispute>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<PayoutItem> PayoutItems => Set<PayoutItem>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<AiUsageDaily> AiUsageDaily => Set<AiUsageDaily>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureDecimalPrecision(modelBuilder);

        modelBuilder.Entity<User>().HasIndex(user => user.Email).IsUnique();
        modelBuilder.Entity<User>()
            .ToTable(user => user.HasCheckConstraint(
                "CK_Users_ActiveRequiresPasswordHash",
                "([Status] <> 4 OR [PasswordHash] IS NOT NULL)"));

        modelBuilder.Entity<Role>().HasIndex(role => role.Name).IsUnique();
        modelBuilder.Entity<UserRoleAssignment>().HasKey(userRole => new { userRole.UserId, userRole.RoleId });

        modelBuilder.Entity<UserRoleAssignment>()
            .HasOne(userRole => userRole.User)
            .WithMany(user => user.Roles)
            .HasForeignKey(userRole => userRole.UserId);

        modelBuilder.Entity<UserRoleAssignment>()
            .HasOne(userRole => userRole.Role)
            .WithMany(role => role.Users)
            .HasForeignKey(userRole => userRole.RoleId);

        modelBuilder.Entity<UserToken>()
            .HasIndex(token => new { token.UserId, token.TokenType, token.TokenHash })
            .IsUnique();

        modelBuilder.Entity<Specialty>().HasIndex(specialty => specialty.Slug).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(product => product.Slug).IsUnique();
        modelBuilder.Entity<ProductCategory>().HasIndex(category => category.Slug).IsUnique();
        modelBuilder.Entity<SubscriptionPlan>().HasIndex(plan => plan.Code).IsUnique();
        modelBuilder.Entity<SystemSetting>().HasIndex(setting => setting.SettingKey).IsUnique();

        modelBuilder.Entity<DoctorProfile>()
            .HasOne(profile => profile.User)
            .WithOne(user => user.DoctorProfile)
            .HasForeignKey<DoctorProfile>(profile => profile.UserId);

        modelBuilder.Entity<PatientProfile>()
            .HasOne(profile => profile.User)
            .WithOne(user => user.PatientProfile)
            .HasForeignKey<PatientProfile>(profile => profile.UserId);

        modelBuilder.Entity<DoctorCredentialDocument>()
            .HasOne(document => document.DoctorProfile)
            .WithMany(profile => profile.CredentialDocuments)
            .HasForeignKey(document => document.DoctorProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DoctorService>()
            .HasIndex(doctorService => new { doctorService.DoctorProfileId, doctorService.MedicalServiceId })
            .IsUnique();

        modelBuilder.Entity<DoctorService>()
            .HasOne(doctorService => doctorService.DoctorProfile)
            .WithMany()
            .HasForeignKey(doctorService => doctorService.DoctorProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DoctorService>()
            .HasOne(doctorService => doctorService.MedicalService)
            .WithMany()
            .HasForeignKey(doctorService => doctorService.MedicalServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DoctorScheduleSlot>()
            .HasIndex(slot => new { slot.DoctorProfileId, slot.StartTime, slot.EndTime })
            .IsUnique();

        modelBuilder.Entity<Appointment>()
            .HasOne(appointment => appointment.DoctorScheduleSlot)
            .WithMany()
            .HasForeignKey(appointment => appointment.DoctorScheduleSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RehabAI.Domain.Entities.Payment>()
            .ToTable(payment => payment.HasCheckConstraint(
                "CK_Payments_ExactlyOneTarget",
                "((CASE WHEN [OrderId] IS NOT NULL THEN 1 ELSE 0 END) + " +
                "(CASE WHEN [AppointmentId] IS NOT NULL THEN 1 ELSE 0 END) + " +
                "(CASE WHEN [SubscriptionId] IS NOT NULL THEN 1 ELSE 0 END)) = 1"));

        modelBuilder.Entity<PaymentWebhookEvent>()
            .HasIndex(webhook => new { webhook.Provider, webhook.ProviderEventId })
            .IsUnique();

        modelBuilder.Entity<PaymentWebhookEvent>()
            .HasOne(webhook => webhook.Payment)
            .WithMany()
            .HasForeignKey(webhook => webhook.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Subscription>()
            .HasIndex(subscription => subscription.UserId)
            .IsUnique()
            .HasFilter("[Status] IN (2, 3, 4)");

        modelBuilder.Entity<AiUsageDaily>()
            .HasIndex(usage => new { usage.UserId, usage.UsageDate })
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL");

        modelBuilder.Entity<AiUsageDaily>()
            .HasIndex(usage => new { usage.GuestSessionId, usage.UsageDate })
            .IsUnique()
            .HasFilter("[GuestSessionId] IS NOT NULL");

        modelBuilder.Entity<PayoutItem>()
            .HasOne(item => item.Payout)
            .WithMany(payout => payout.Items)
            .HasForeignKey(item => item.PayoutId);
    }

    private static void ConfigureDecimalPrecision(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var clrType = property.ClrType;
                var isDecimal = clrType == typeof(decimal) || clrType == typeof(decimal?);

                if (!isDecimal)
                {
                    continue;
                }

                property.SetPrecision(18);
                property.SetScale(property.Name == "CostAmount" ? 6 : 2);
            }
        }
    }
}
