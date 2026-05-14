using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RehabAI.Application.Chatbot;
using RehabAI.Application.Emails;
using RehabAI.Application.Payments;
using RehabAI.Infrastructure.Ai;
using RehabAI.Infrastructure.Database;
using RehabAI.Infrastructure.Email;
using RehabAI.Infrastructure.Payment;

namespace RehabAI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IEmailSender, PlaceholderEmailSender>();
        services.AddScoped<IPaymentGateway, PlaceholderPaymentGateway>();
        services.AddScoped<IAiChatClient, PlaceholderAiChatClient>();

        return services;
    }
}
