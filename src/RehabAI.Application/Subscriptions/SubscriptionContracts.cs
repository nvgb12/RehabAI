using RehabAI.Domain.Enums;

namespace RehabAI.Application.Subscriptions;

public sealed record SubscribeCommand(Guid CurrentUserId, Guid PlanId);

public sealed record SubscriptionPlanResponse(
    Guid PlanId,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int DurationDays,
    bool IsActive);

public sealed record SubscriptionResponse(
    Guid SubscriptionId,
    Guid PlanId,
    string PlanName,
    string Status,
    string PaymentStatus,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    decimal Price,
    string Currency);

public sealed record SubscriptionResult(
    bool Succeeded,
    string Message,
    SubscriptionResponse? Subscription = null,
    SubscriptionFailureReason? FailureReason = null);

public sealed record CurrentSubscriptionResult(
    bool Succeeded,
    string Message,
    SubscriptionResponse? Subscription = null,
    SubscriptionFailureReason? FailureReason = null);

public enum SubscriptionFailureReason
{
    Validation = 1,
    AccessDenied = 2,
    PlanNotFound = 3,
    SubscriptionNotFound = 4,
    SubscriptionAlreadyPaid = 5,
    CurrentSubscriptionExists = 6
}

public interface ISubscriptionService
{
    Task<IReadOnlyList<SubscriptionPlanResponse>> GetActivePlansAsync(
        CancellationToken cancellationToken = default);
    Task<CurrentSubscriptionResult> GetCurrentSubscriptionAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default);
    Task<SubscriptionResult> SubscribeAsync(
        SubscribeCommand command,
        CancellationToken cancellationToken = default);
    Task<SubscriptionResult> ConfirmPaymentAsync(
        Guid currentUserId,
        Guid subscriptionId,
        CancellationToken cancellationToken = default);
}

public interface ISubscriptionRepository
{
    Task<IReadOnlyList<SubscriptionPlanRecord>> GetActivePlansAsync(
        CancellationToken cancellationToken = default);
    Task<SubscriptionUserState?> GetUserStateAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<SubscriptionPlanRecord?> GetPlanByIdAsync(
        Guid planId,
        CancellationToken cancellationToken = default);
    Task<bool> HasCurrentSubscriptionAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<SubscriptionRecord?> GetCurrentSubscriptionAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<SubscriptionRecord> CreatePendingSubscriptionAsync(
        CreateSubscriptionDraft draft,
        CancellationToken cancellationToken = default);
    Task<SubscriptionRepositoryResult> ConfirmPaymentPlaceholderAsync(
        Guid userId,
        Guid subscriptionId,
        int durationDays,
        CancellationToken cancellationToken = default);
}

public sealed record SubscriptionUserState(
    Guid UserId,
    int Status,
    bool HasPatientRole);

public sealed record SubscriptionPlanRecord(
    Guid PlanId,
    string Code,
    string Name,
    decimal Price,
    bool IsActive,
    bool IsDeleted);

public sealed record CreateSubscriptionDraft(
    Guid UserId,
    Guid PlanId,
    string PlanCodeSnapshot,
    decimal Price,
    string Currency);

public sealed record SubscriptionRecord(
    Guid SubscriptionId,
    Guid UserId,
    Guid PlanId,
    string PlanName,
    SubscriptionStatus Status,
    PaymentStatus PaymentStatus,
    DateTimeOffset? PaidAt,
    DateTimeOffset? CurrentPeriodEnd,
    decimal Price,
    string Currency);

public sealed record SubscriptionRepositoryResult(
    bool Succeeded,
    SubscriptionRecord? Subscription,
    SubscriptionFailureReason? FailureReason,
    string? Message);
