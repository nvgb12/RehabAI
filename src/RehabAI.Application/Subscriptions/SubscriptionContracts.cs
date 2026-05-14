namespace RehabAI.Application.Subscriptions;

public interface ISubscriptionService
{
    Task<string> SubscribeToProAsync(Guid patientUserId, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid patientUserId, CancellationToken cancellationToken = default);
}
