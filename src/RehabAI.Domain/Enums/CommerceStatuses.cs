namespace RehabAI.Domain.Enums;

public enum PaymentStatus
{
    Pending = 1,
    Paid = 2,
    Failed = 3,
    RefundPending = 4,
    Refunded = 5,
    RefundFailed = 6,
    Cancelled = 7
}

public enum PaymentPurpose
{
    Order = 1,
    Appointment = 2,
    Subscription = 3,
    DoctorApplicationFee = 4,
    NoShowFee = 5
}

public enum WebhookProcessingStatus
{
    Pending = 1,
    Processed = 2,
    Failed = 3,
    IgnoredDuplicate = 4
}

public enum RefundStatus
{
    Pending = 1,
    Refunded = 2,
    Failed = 3
}

public enum OrderStatus
{
    PendingPayment = 1,
    Paid = 2,
    Processing = 3,
    Shipped = 4,
    Completed = 5,
    Cancelled = 6,
    Refunded = 7
}

public enum SubscriptionStatus
{
    Inactive = 1,
    Active = 2,
    PastDue = 3,
    Cancelled = 4,
    Expired = 5
}

public enum DisputeStatus
{
    Open = 1,
    UnderReview = 2,
    Resolved = 3,
    Rejected = 4
}

public enum PayoutStatus
{
    Pending = 1,
    Processing = 2,
    Paid = 3,
    Failed = 4
}

public enum ReviewStatus
{
    Visible = 1,
    Hidden = 2,
    Flagged = 3,
    Removed = 4
}

public enum UserTokenType
{
    EmailVerification = 1,
    PasswordReset = 2,
    DoctorInvitation = 3
}

public enum ChatMessageRole
{
    User = 1,
    Assistant = 2,
    System = 3,
    Tool = 4
}
