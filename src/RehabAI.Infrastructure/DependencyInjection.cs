using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RehabAI.Application.Auth;
using RehabAI.Application.Chatbot;
using RehabAI.Application.Appointments;
using RehabAI.Application.Doctors;
using RehabAI.Application.DoctorSchedules;
using RehabAI.Application.Emails;
using RehabAI.Application.MedicalServices;
using RehabAI.Application.PatientProfiles;
using RehabAI.Application.Payments;
using RehabAI.Infrastructure.Ai;
using RehabAI.Infrastructure.Appointments;
using RehabAI.Infrastructure.Auth;
using RehabAI.Infrastructure.Database;
using RehabAI.Infrastructure.Doctors;
using RehabAI.Infrastructure.DoctorSchedules;
using RehabAI.Infrastructure.Email;
using RehabAI.Infrastructure.MedicalServices;
using RehabAI.Infrastructure.PatientProfiles;
using RehabAI.Infrastructure.Payment;

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
        services.AddScoped<IPublicDoctorListingService, PublicDoctorListingService>();
        services.AddScoped<IDoctorScheduleSlotService, DoctorScheduleSlotService>();
        services.AddScoped<IMedicalServiceManager, MedicalServiceManager>();
        services.AddScoped<IPatientProfileService, PatientProfileService>();
        services.AddScoped<IPatientRegistrationRepository, EfPatientRegistrationRepository>();
        services.AddScoped<IUserAuthenticationRepository, EfPatientRegistrationRepository>();
        services.AddScoped<IDoctorAccountRepository, EfDoctorAccountRepository>();
        services.AddScoped<IPublicDoctorListingRepository, EfPublicDoctorListingRepository>();
        services.AddScoped<IDoctorScheduleSlotRepository, EfDoctorScheduleSlotRepository>();
        services.AddScoped<IMedicalServiceRepository, EfMedicalServiceRepository>();
        services.AddScoped<IAppointmentBookingRepository, EfAppointmentBookingRepository>();
        services.AddScoped<IPatientProfileRepository, EfPatientProfileRepository>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ISecureTokenService, SecureTokenService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IEmailSender, PlaceholderEmailSender>();
        services.AddScoped<IPaymentGateway, PlaceholderPaymentGateway>();
        services.AddScoped<IAiChatClient, PlaceholderAiChatClient>();

        return services;
    }
}
