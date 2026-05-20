namespace RehabAI.Api.Contracts.Orders;

public sealed record CreateOrderRequest(
    Guid PatientProfileId,
    IReadOnlyList<CreateOrderItemRequest>? Items,
    string? ShippingAddress);

public sealed record CreateOrderItemRequest(
    Guid ProductId,
    int Quantity);

public sealed record UpdateOrderStatusRequest(
    string Status);
