namespace RehabAI.Application.Payments;

public interface IPaymentGateway
{
    Task<string> CreateCheckoutSessionAsync(Guid paymentTargetId, decimal amount, string currency, CancellationToken cancellationToken = default);
    Task<bool> VerifyWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default);
}
