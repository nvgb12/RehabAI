using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RehabAI.Application.Auth;
using RehabAI.Application.Chatbot;
using RehabAI.Application.Appointments;
using RehabAI.Application.Doctors;
using RehabAI.Application.DoctorSchedules;
using RehabAI.Application.Emails;
using RehabAI.Application.Lookups;
using RehabAI.Application.MedicalServices;
using RehabAI.Application.Orders;
using RehabAI.Application.PatientProfiles;
using RehabAI.Application.Payments;
using RehabAI.Application.Products;
using RehabAI.Application.Reports;
using RehabAI.Application.Subscriptions;
using RehabAI.Infrastructure.Ai;
using RehabAI.Infrastructure.Appointments;
using RehabAI.Infrastructure.Auth;
using RehabAI.Infrastructure.Database;
using RehabAI.Infrastructure.Doctors;
using RehabAI.Infrastructure.DoctorSchedules;
using RehabAI.Infrastructure.Email;
using RehabAI.Infrastructure.Lookups;
using RehabAI.Infrastructure.MedicalServices;
using RehabAI.Infrastructure.Orders;
using RehabAI.Infrastructure.PatientProfiles;
using RehabAI.Infrastructure.Payment;
using RehabAI.Infrastructure.Products;
using RehabAI.Infrastructure.Reports;
using RehabAI.Infrastructure.Subscriptions;

namespace RehabAI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAppointmentBookingService, AppointmentBookingService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IDoctorDashboardService, DoctorDashboardService>();
        services.AddScoped<IPublicDoctorListingService, PublicDoctorListingService>();
        services.AddScoped<IDoctorScheduleSlotService, DoctorScheduleSlotService>();
        services.AddScoped<ILookupService, LookupService>();
        services.AddScoped<IMedicalServiceManager, MedicalServiceManager>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPatientProfileService, PatientProfileService>();
        services.AddScoped<IProductManager, ProductManager>();
        services.AddScoped<IRevenueReportService, RevenueReportService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IPatientRegistrationRepository, EfPatientRegistrationRepository>();
        services.AddScoped<IUserAuthenticationRepository, EfPatientRegistrationRepository>();
        services.AddScoped<IDoctorAccountRepository, EfDoctorAccountRepository>();
        services.AddScoped<IDoctorDashboardRepository, EfDoctorDashboardRepository>();
        services.AddScoped<IPublicDoctorListingRepository, EfPublicDoctorListingRepository>();
        services.AddScoped<IDoctorScheduleSlotRepository, EfDoctorScheduleSlotRepository>();
        services.AddScoped<ILookupRepository, EfLookupRepository>();
        services.AddScoped<IMedicalServiceRepository, EfMedicalServiceRepository>();
        services.AddScoped<IAppointmentBookingRepository, EfAppointmentBookingRepository>();
        services.AddScoped<IOrderRepository, EfOrderRepository>();
        services.AddScoped<IPatientProfileRepository, EfPatientProfileRepository>();
        services.AddScoped<IProfileImageStorage, LocalProfileImageStorage>();
        services.AddScoped<IDoctorAvatarStorage, LocalDoctorAvatarStorage>();
        services.AddScoped<IProductRepository, EfProductRepository>();
        services.AddScoped<IRevenueReportRepository, EfRevenueReportRepository>();
        services.AddScoped<ISubscriptionRepository, EfSubscriptionRepository>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ISecureTokenService, SecureTokenService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IEmailSender, PlaceholderEmailSender>();
        services.AddScoped<IPaymentGateway, PlaceholderPaymentGateway>();
        services.AddScoped<IAiChatClient, PlaceholderAiChatClient>();

        return services;
    }
}
