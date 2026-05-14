using Microsoft.Extensions.Logging;
using RehabAI.Application.Emails;

namespace RehabAI.Infrastructure.Email;

public class PlaceholderEmailSender(ILogger<PlaceholderEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Email placeholder: {Subject} to {ToEmail}", subject, toEmail);
        return Task.CompletedTask;
    }
}
