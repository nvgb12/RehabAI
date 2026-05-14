using RehabAI.Application.Payments;

namespace RehabAI.Infrastructure.Payment;

public class PlaceholderPaymentGateway : IPaymentGateway
{
    public Task<string> CreateCheckoutSessionAsync(Guid paymentTargetId, decimal amount, string currency, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"https://payment.example/checkout/{paymentTargetId}?amount={amount}&currency={currency}");
    }

    public Task<bool> VerifyWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(!string.IsNullOrWhiteSpace(payload) && !string.IsNullOrWhiteSpace(signature));
    }
}
