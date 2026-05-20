using RehabAI.Domain.Enums;

namespace RehabAI.Application.Subscriptions;

public sealed class SubscriptionService(ISubscriptionRepository repository) : ISubscriptionService
{
    private const string DefaultCurrency = "VND";
    private const int DefaultDurationDays = 30;

    public async Task<IReadOnlyList<SubscriptionPlanResponse>> GetActivePlansAsync(
        CancellationToken cancellationToken = default)
    {
        var plans = await repository.GetActivePlansAsync(cancellationToken);

        return plans
            .Select(ToPlanResponse)
            .ToList();
    }

    public async Task<CurrentSubscriptionResult> GetCurrentSubscriptionAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var accessResult = await ValidatePatientAccessAsync(currentUserId, cancellationToken);

        if (!accessResult.Succeeded)
        {
            return new CurrentSubscriptionResult(
                false,
                accessResult.Message,
                FailureReason: accessResult.FailureReason);
        }

        var subscription = await repository.GetCurrentSubscriptionAsync(currentUserId, cancellationToken);

        return new CurrentSubscriptionResult(
            true,
            subscription is null ? "No current subscription was found." : "Subscription retrieved successfully.",
            subscription is null ? null : ToSubscriptionResponse(subscription));
    }

    public async Task<SubscriptionResult> SubscribeAsync(
        SubscribeCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.PlanId == Guid.Empty)
        {
            return Failed("Plan id is required.", SubscriptionFailureReason.Validation);
        }

        var accessResult = await ValidatePatientAccessAsync(command.CurrentUserId, cancellationToken);

        if (!accessResult.Succeeded)
        {
            return Failed(accessResult.Message, accessResult.FailureReason);
        }

        var plan = await repository.GetPlanByIdAsync(command.PlanId, cancellationToken);

        if (plan is null || plan.IsDeleted || !plan.IsActive)
        {
            return Failed("Subscription plan was not found.", SubscriptionFailureReason.PlanNotFound);
        }

        if (await repository.HasCurrentSubscriptionAsync(command.CurrentUserId, cancellationToken))
        {
            return Failed(
                "User already has a current subscription.",
                SubscriptionFailureReason.CurrentSubscriptionExists);
        }

        var subscription = await repository.CreatePendingSubscriptionAsync(
            new CreateSubscriptionDraft(
                command.CurrentUserId,
                plan.PlanId,
                plan.Code,
                plan.Price,
                DefaultCurrency),
            cancellationToken);

        return new SubscriptionResult(
            true,
            "Subscription created and is pending payment.",
            ToSubscriptionResponse(subscription));
    }

    public async Task<SubscriptionResult> ConfirmPaymentAsync(
        Guid currentUserId,
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        if (subscriptionId == Guid.Empty)
        {
            return Failed("Subscription id is required.", SubscriptionFailureReason.Validation);
        }

        var accessResult = await ValidatePatientAccessAsync(currentUserId, cancellationToken);

        if (!accessResult.Succeeded)
        {
            return Failed(accessResult.Message, accessResult.FailureReason);
        }

        var result = await repository.ConfirmPaymentPlaceholderAsync(
            currentUserId,
            subscriptionId,
            DefaultDurationDays,
            cancellationToken);

        if (!result.Succeeded)
        {
            return Failed(
                result.Message ?? "Subscription payment could not be confirmed.",
                result.FailureReason ?? SubscriptionFailureReason.Validation);
        }

        return new SubscriptionResult(
            true,
            "Subscription payment confirmed successfully.",
            ToSubscriptionResponse(result.Subscription!));
    }

    private async Task<SubscriptionAccessResult> ValidatePatientAccessAsync(
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        if (currentUserId == Guid.Empty)
        {
            return SubscriptionAccessResult.Failed(
                "Authenticated user is required.",
                SubscriptionFailureReason.AccessDenied);
        }

        var user = await repository.GetUserStateAsync(currentUserId, cancellationToken);

        if (user is null ||
            user.Status != (int)AccountStatus.Active ||
            !user.HasPatientRole)
        {
            return SubscriptionAccessResult.Failed(
                "Only active Patient users can manage subscriptions.",
                SubscriptionFailureReason.AccessDenied);
        }

        return SubscriptionAccessResult.Success();
    }

    private static SubscriptionPlanResponse ToPlanResponse(SubscriptionPlanRecord record)
    {
        return new SubscriptionPlanResponse(
            record.PlanId,
            record.Name,
            $"{record.Name} subscription plan.",
            record.Price,
            DefaultCurrency,
            DefaultDurationDays,
            record.IsActive);
    }

    private static SubscriptionResponse ToSubscriptionResponse(SubscriptionRecord record)
    {
        return new SubscriptionResponse(
            record.SubscriptionId,
            record.PlanId,
            record.PlanName,
            ToResponseStatus(record.Status, record.PaymentStatus),
            record.PaymentStatus.ToString(),
            record.PaidAt,
            record.CurrentPeriodEnd,
            record.Price,
            record.Currency);
    }

    private static string ToResponseStatus(SubscriptionStatus status, PaymentStatus paymentStatus)
    {
        return status == SubscriptionStatus.Inactive && paymentStatus == PaymentStatus.Pending
            ? "PendingPayment"
            : status.ToString();
    }

    private static SubscriptionResult Failed(string message, SubscriptionFailureReason reason)
    {
        return new SubscriptionResult(false, message, FailureReason: reason);
    }

    private sealed record SubscriptionAccessResult(
        bool Succeeded,
        string Message,
        SubscriptionFailureReason FailureReason)
    {
        public static SubscriptionAccessResult Success()
        {
            return new SubscriptionAccessResult(true, string.Empty, SubscriptionFailureReason.Validation);
        }

        public static SubscriptionAccessResult Failed(string message, SubscriptionFailureReason reason)
        {
            return new SubscriptionAccessResult(false, message, reason);
        }
    }
}
