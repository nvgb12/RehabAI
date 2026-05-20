using System.Data;
using Microsoft.EntityFrameworkCore;
using RehabAI.Application.Subscriptions;
using RehabAI.Domain.Entities;
using RehabAI.Domain.Enums;
using RehabAI.Infrastructure.Database;
using PaymentEntity = RehabAI.Domain.Entities.Payment;

namespace RehabAI.Infrastructure.Subscriptions;

public sealed class EfSubscriptionRepository(AppDbContext dbContext) : ISubscriptionRepository
{
    private const string DefaultCurrency = "VND";
    private const string PlaceholderProvider = "Placeholder";

    public async Task<IReadOnlyList<SubscriptionPlanRecord>> GetActivePlansAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SubscriptionPlans
            .AsNoTracking()
            .Where(plan => plan.IsActive && !plan.IsDeleted)
            .OrderBy(plan => plan.Price)
            .ThenBy(plan => plan.Name)
            .Select(plan => new SubscriptionPlanRecord(
                plan.Id,
                plan.Code,
                plan.Name,
                plan.Price,
                plan.IsActive,
                plan.IsDeleted))
            .ToListAsync(cancellationToken);
    }

    public async Task<SubscriptionUserState?> GetUserStateAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(user =>
                user.Id == userId &&
                !user.IsDeleted &&
                user.PatientProfile != null &&
                !user.PatientProfile.IsDeleted)
            .Select(user => new SubscriptionUserState(
                user.Id,
                (int)user.Status,
                user.Roles.Any(userRole =>
                    userRole.Role != null &&
                    userRole.Role.Name == "Patient" &&
                    !userRole.Role.IsDeleted)))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<SubscriptionPlanRecord?> GetPlanByIdAsync(
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SubscriptionPlans
            .AsNoTracking()
            .Where(plan => plan.Id == planId)
            .Select(plan => new SubscriptionPlanRecord(
                plan.Id,
                plan.Code,
                plan.Name,
                plan.Price,
                plan.IsActive,
                plan.IsDeleted))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> HasCurrentSubscriptionAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Subscriptions
            .AsNoTracking()
            .AnyAsync(
                subscription =>
                    subscription.UserId == userId &&
                    !subscription.IsDeleted &&
                    (subscription.Status == SubscriptionStatus.Active ||
                        subscription.Status == SubscriptionStatus.PastDue ||
                        subscription.Status == SubscriptionStatus.Cancelled),
                cancellationToken);
    }

    public async Task<SubscriptionRecord?> GetCurrentSubscriptionAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var subscriptions = await dbContext.Subscriptions
            .AsNoTracking()
            .Include(subscription => subscription.Plan)
            .Where(subscription => subscription.UserId == userId && !subscription.IsDeleted)
            .ToListAsync(cancellationToken);

        var subscription = subscriptions
            .OrderByDescending(IsCurrentSubscription)
            .ThenByDescending(item => item.CreatedAt)
            .FirstOrDefault();

        if (subscription is null)
        {
            return null;
        }

        var payment = await GetLatestPaymentAsync(subscription.Id, cancellationToken);

        return ToRecord(subscription, payment);
    }

    public async Task<SubscriptionRecord> CreatePendingSubscriptionAsync(
        CreateSubscriptionDraft draft,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var subscription = new Subscription
        {
            UserId = draft.UserId,
            PlanId = draft.PlanId,
            PlanCodeSnapshot = draft.PlanCodeSnapshot,
            Status = SubscriptionStatus.Inactive,
            CreatedAt = now
        };

        var payment = new PaymentEntity
        {
            Purpose = PaymentPurpose.Subscription,
            SubscriptionId = subscription.Id,
            Provider = PlaceholderProvider,
            ProviderSessionId = $"SUB-{subscription.Id:N}",
            Amount = draft.Price,
            Currency = NormalizeCurrency(draft.Currency),
            Status = PaymentStatus.Pending,
            CreatedAt = now
        };

        dbContext.Subscriptions.Add(subscription);
        dbContext.Payments.Add(payment);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        subscription.Plan = await dbContext.SubscriptionPlans
            .AsNoTracking()
            .SingleAsync(plan => plan.Id == subscription.PlanId, cancellationToken);

        return ToRecord(subscription, payment);
    }

    public async Task<SubscriptionRepositoryResult> ConfirmPaymentPlaceholderAsync(
        Guid userId,
        Guid subscriptionId,
        int durationDays,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var subscription = await dbContext.Subscriptions
            .Include(item => item.Plan)
            .SingleOrDefaultAsync(
                item =>
                    item.Id == subscriptionId &&
                    item.UserId == userId &&
                    !item.IsDeleted,
                cancellationToken);

        if (subscription is null)
        {
            return Failed("Subscription was not found.", SubscriptionFailureReason.SubscriptionNotFound);
        }

        if (await HasOtherCurrentSubscriptionAsync(userId, subscriptionId, cancellationToken))
        {
            return Failed(
                "User already has a current subscription.",
                SubscriptionFailureReason.CurrentSubscriptionExists);
        }

        var payment = await dbContext.Payments
            .Where(item =>
                item.SubscriptionId == subscriptionId &&
                item.Purpose == PaymentPurpose.Subscription &&
                !item.IsDeleted)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription.Status == SubscriptionStatus.Active ||
            payment?.Status == PaymentStatus.Paid)
        {
            return Failed(
                "Subscription payment has already been confirmed.",
                SubscriptionFailureReason.SubscriptionAlreadyPaid);
        }

        if (payment is null || payment.Status != PaymentStatus.Pending)
        {
            return Failed(
                "Only subscriptions with Pending payment status can be confirmed.",
                SubscriptionFailureReason.SubscriptionAlreadyPaid);
        }

        var now = DateTimeOffset.UtcNow;

        payment.Status = PaymentStatus.Paid;
        payment.PaidAt = now;
        payment.UpdatedAt = now;

        subscription.Status = SubscriptionStatus.Active;
        subscription.CurrentPeriodEnd = now.AddDays(durationDays);
        subscription.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new SubscriptionRepositoryResult(
            true,
            ToRecord(subscription, payment),
            null,
            null);
    }

    private async Task<bool> HasOtherCurrentSubscriptionAsync(
        Guid userId,
        Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Subscriptions
            .AnyAsync(
                subscription =>
                    subscription.UserId == userId &&
                    subscription.Id != subscriptionId &&
                    !subscription.IsDeleted &&
                    (subscription.Status == SubscriptionStatus.Active ||
                        subscription.Status == SubscriptionStatus.PastDue ||
                        subscription.Status == SubscriptionStatus.Cancelled),
                cancellationToken);
    }

    private async Task<PaymentEntity?> GetLatestPaymentAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        return await dbContext.Payments
            .AsNoTracking()
            .Where(payment =>
                payment.SubscriptionId == subscriptionId &&
                payment.Purpose == PaymentPurpose.Subscription &&
                !payment.IsDeleted)
            .OrderByDescending(payment => payment.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static SubscriptionRecord ToRecord(Subscription subscription, PaymentEntity? payment)
    {
        var paymentStatus = payment?.Status ??
            (subscription.Status == SubscriptionStatus.Active ? PaymentStatus.Paid : PaymentStatus.Pending);

        return new SubscriptionRecord(
            subscription.Id,
            subscription.UserId,
            subscription.PlanId,
            subscription.Plan?.Name ?? subscription.PlanCodeSnapshot,
            subscription.Status,
            paymentStatus,
            payment?.PaidAt,
            subscription.CurrentPeriodEnd,
            payment?.Amount ?? subscription.Plan?.Price ?? 0m,
            NormalizeCurrency(payment?.Currency));
    }

    private static bool IsCurrentSubscription(Subscription subscription)
    {
        return subscription.Status is
            SubscriptionStatus.Active or
            SubscriptionStatus.PastDue or
            SubscriptionStatus.Cancelled;
    }

    private static string NormalizeCurrency(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DefaultCurrency : value.Trim().ToUpperInvariant();
    }

    private static SubscriptionRepositoryResult Failed(
        string message,
        SubscriptionFailureReason failureReason)
    {
        return new SubscriptionRepositoryResult(false, null, failureReason, message);
    }
}
