namespace RehabAI.Application.Orders;

public interface IOrderService
{
    Task<Guid> PlaceOrderAsync(Guid patientUserId, CancellationToken cancellationToken = default);
    Task CancelOrderAsync(Guid orderId, Guid patientUserId, CancellationToken cancellationToken = default);
}
