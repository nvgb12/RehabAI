using RehabAI.Domain.Enums;

namespace RehabAI.Domain.Entities;

public class ProductCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class Product : BaseEntity
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public int StockQuantity { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Cart : BaseEntity
{
    public Guid UserId { get; set; }
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}

public class CartItem : BaseEntity
{
    public Guid CartId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class Order : BaseEntity
{
    public Guid UserId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public string? ShippingAddress { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}

public class Payment : BaseEntity
{
    public PaymentPurpose Purpose { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? ProviderSessionId { get; set; }
    public string? ProviderPaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? FailedAt { get; set; }
    public string? FailureReason { get; set; }
}

public class PaymentWebhookEvent : BaseEntity
{
    public string Provider { get; set; } = string.Empty;
    public string ProviderEventId { get; set; } = string.Empty;
    public Guid? PaymentId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public WebhookProcessingStatus ProcessingStatus { get; set; } = WebhookProcessingStatus.Pending;
    public DateTimeOffset? ProcessedAt { get; set; }
    public Payment? Payment { get; set; }
}

public class Refund : BaseEntity
{
    public Guid PaymentId { get; set; }
    public string? ProviderRefundId { get; set; }
    public decimal Amount { get; set; }
    public RefundStatus Status { get; set; } = RefundStatus.Pending;
    public string? Reason { get; set; }
}

public class SubscriptionPlan : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DailyMessageLimit { get; set; }
    public int HistoryRetentionDays { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Subscription : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public string PlanCodeSnapshot { get; set; } = "Free";
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Inactive;
    public DateTimeOffset? CurrentPeriodEnd { get; set; }
    public SubscriptionPlan? Plan { get; set; }
}

public class Payout : BaseEntity
{
    public Guid DoctorProfileId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetAmount { get; set; }
    public PayoutStatus Status { get; set; } = PayoutStatus.Pending;
    public ICollection<PayoutItem> Items { get; set; } = new List<PayoutItem>();
}

public class PayoutItem : BaseEntity
{
    public Guid PayoutId { get; set; }
    public Guid AppointmentId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetAmount { get; set; }
    public Payout? Payout { get; set; }
}
