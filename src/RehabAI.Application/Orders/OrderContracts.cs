using RehabAI.Domain.Enums;

namespace RehabAI.Application.Orders;

public sealed record CreateOrderCommand(
    Guid PatientProfileId,
    IReadOnlyList<CreateOrderItemCommand> Items,
    string? ShippingAddress);

public sealed record CreateOrderItemCommand(
    Guid ProductId,
    int Quantity);

public sealed record AdminOrderQuery(
    string? Status,
    string? PaymentStatus,
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate);

public sealed record AdminOrderFilter(
    OrderStatus? Status,
    PaymentStatus? PaymentStatus,
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate);

public sealed record UpdateOrderStatusCommand(
    string Status);

public sealed record CustomerOrderAccessState(
    Guid UserId,
    Guid PatientProfileId,
    int Status,
    bool HasPatientRole);

public static class OrderStatusCatalog
{
    private static readonly OrderStatus[] AllStatusValues = Enum.GetValues<OrderStatus>();
    private static readonly OrderStatus[] AdminUpdateStatusValues =
    [
        OrderStatus.Paid,
        OrderStatus.Processing,
        OrderStatus.Shipped,
        OrderStatus.Completed,
        OrderStatus.Cancelled
    ];

    public static IReadOnlyList<OrderStatus> AllStatuses => AllStatusValues;
    public static IReadOnlyList<OrderStatus> AdminUpdateStatuses => AdminUpdateStatusValues;
    public static IReadOnlyList<string> AdminUpdateStatusNames => AdminUpdateStatusValues
        .Select(status => status.ToString())
        .ToList();

    public static string AllStatusValuesText => Format(AllStatusValues);
    public static string AdminUpdateStatusValuesText => Format(AdminUpdateStatusValues);

    private static string Format(IEnumerable<OrderStatus> statuses)
    {
        return string.Join(", ", statuses.Select(status => status.ToString()));
    }
}

public sealed record OrderResponse(
    Guid OrderId,
    Guid PatientProfileId,
    Guid PatientUserId,
    string OrderNumber,
    string Status,
    string PaymentStatus,
    decimal TotalAmount,
    string Currency,
    string? ShippingAddress,
    IReadOnlyList<OrderItemResponse> Items);

public sealed record OrderItemResponse(
    Guid OrderItemId,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);

public sealed record CustomerOrderSummaryResponse(
    Guid OrderId,
    string OrderNumber,
    DateTimeOffset CreatedAt,
    decimal TotalAmount,
    string Currency,
    string Status,
    string PaymentStatus);

public sealed record CustomerOrderDetailResponse(
    Guid OrderId,
    string OrderNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string? ShippingAddress,
    decimal TotalAmount,
    string Currency,
    string Status,
    string PaymentStatus,
    IReadOnlyList<CustomerOrderItemResponse> Items);

public sealed record CustomerOrderItemResponse(
    Guid OrderItemId,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);

public sealed record OrderResult(
    bool Succeeded,
    string Message,
    OrderResponse? Order = null,
    OrderFailureReason? FailureReason = null);

public sealed record AdminOrderListResult(
    bool Succeeded,
    string Message,
    IReadOnlyList<AdminOrderSummaryResponse> Orders,
    OrderFailureReason? FailureReason = null);

public sealed record AdminOrderResult(
    bool Succeeded,
    string Message,
    AdminOrderDetailResponse? Order = null,
    OrderFailureReason? FailureReason = null);

public sealed record CustomerOrderListResult(
    bool Succeeded,
    string Message,
    IReadOnlyList<CustomerOrderSummaryResponse> Orders,
    OrderFailureReason? FailureReason = null);

public sealed record CustomerOrderDetailResult(
    bool Succeeded,
    string Message,
    CustomerOrderDetailResponse? Order = null,
    OrderFailureReason? FailureReason = null);

public sealed record AdminOrderSummaryResponse(
    Guid OrderId,
    string OrderNumber,
    Guid PatientProfileId,
    string? PatientName,
    string? PatientEmail,
    string Status,
    string PaymentStatus,
    decimal TotalAmount,
    string Currency,
    string? ShippingAddress,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record AdminOrderDetailResponse(
    Guid OrderId,
    string OrderNumber,
    Guid PatientProfileId,
    string? PatientName,
    string? PatientEmail,
    string Status,
    string PaymentStatus,
    decimal TotalAmount,
    string Currency,
    string? ShippingAddress,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<OrderItemResponse> Items);

public enum OrderFailureReason
{
    Validation = 1,
    PatientNotFound = 2,
    ProductNotFound = 3,
    ProductUnavailable = 4,
    QuantityExceedsStock = 5,
    OrderNotFound = 6,
    OrderPaymentNotPending = 7,
    InvalidStatus = 8,
    InvalidStatusTransition = 9,
    AccessDenied = 10
}

public interface IOrderService
{
    Task<OrderResult> CreateAsync(CreateOrderCommand command, CancellationToken cancellationToken = default);
    Task<OrderResult> ConfirmPaymentAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<OrderResponse?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderResponse>> GetPatientOrdersAsync(
        Guid patientProfileId,
        CancellationToken cancellationToken = default);
    Task<CustomerOrderListResult> GetMyOrdersAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default);
    Task<CustomerOrderDetailResult> GetMyOrderByIdAsync(
        Guid currentUserId,
        Guid orderId,
        CancellationToken cancellationToken = default);
    Task<AdminOrderListResult> GetAdminOrdersAsync(
        AdminOrderQuery query,
        CancellationToken cancellationToken = default);
    Task<AdminOrderDetailResponse?> GetAdminOrderByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);
    Task<AdminOrderResult> UpdateAdminOrderStatusAsync(
        Guid orderId,
        UpdateOrderStatusCommand command,
        CancellationToken cancellationToken = default);
}

public interface IOrderRepository
{
    Task<OrderPatientState?> GetPatientStateAsync(Guid patientProfileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductOrderState>> GetProductStatesAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default);
    Task<OrderRecord> CreateAsync(OrderDraft draft, CancellationToken cancellationToken = default);
    Task<OrderRepositoryResult> ConfirmPaymentPlaceholderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);
    Task<OrderRecord?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderRecord>> GetByPatientProfileIdAsync(
        Guid patientProfileId,
        CancellationToken cancellationToken = default);
    Task<CustomerOrderAccessState?> GetCustomerOrderAccessStateAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerOrderSummaryRecord>> GetCustomerOrdersByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<CustomerOrderDetailRecord?> GetCustomerOrderByIdAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminOrderRecord>> GetAdminOrdersAsync(
        AdminOrderFilter filter,
        CancellationToken cancellationToken = default);
    Task<AdminOrderRecord?> GetAdminOrderByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);
    Task<AdminOrderRecord?> UpdateAdminOrderStatusAsync(
        Guid orderId,
        OrderStatus status,
        CancellationToken cancellationToken = default);
}

public sealed record OrderPatientState(
    Guid PatientProfileId,
    Guid UserId);

public sealed record ProductOrderState(
    Guid ProductId,
    string Name,
    decimal Price,
    string Currency,
    int StockQuantity,
    bool IsActive,
    bool IsDeleted);

public sealed record OrderDraft(
    Guid PatientProfileId,
    Guid PatientUserId,
    string OrderNumber,
    string Currency,
    decimal TotalAmount,
    string? ShippingAddress,
    IReadOnlyList<OrderItemDraft> Items);

public sealed record OrderItemDraft(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);

public sealed record OrderRecord(
    Guid OrderId,
    Guid PatientProfileId,
    Guid PatientUserId,
    string OrderNumber,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    decimal TotalAmount,
    string Currency,
    string? ShippingAddress,
    IReadOnlyList<OrderItemRecord> Items);

public sealed record CustomerOrderSummaryRecord(
    Guid OrderId,
    string OrderNumber,
    DateTimeOffset CreatedAt,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    decimal TotalAmount,
    string Currency);

public sealed record CustomerOrderDetailRecord(
    Guid OrderId,
    string OrderNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string? ShippingAddress,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    decimal TotalAmount,
    string Currency,
    IReadOnlyList<OrderItemRecord> Items);

public sealed record OrderRepositoryResult(
    bool Succeeded,
    OrderRecord? Order,
    OrderFailureReason? FailureReason,
    string? Message);

public sealed record OrderItemRecord(
    Guid OrderItemId,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);

public sealed record AdminOrderRecord(
    Guid OrderId,
    string OrderNumber,
    Guid PatientProfileId,
    string? PatientName,
    string? PatientEmail,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    decimal TotalAmount,
    string Currency,
    string? ShippingAddress,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<OrderItemRecord> Items);
