using RehabAI.Application.Subscriptions;
using RehabAI.Domain.Enums;

namespace RehabAI.UnitTests.Subscriptions;

public class SubscriptionServiceTests
{
    [Fact]
    public async Task GetActivePlansAsync_ReturnsActiveNonDeletedPlans()
    {
        var repository = new FakeSubscriptionRepository();
        var service = new SubscriptionService(repository);

        var plans = await service.GetActivePlansAsync();

        Assert.Contains(plans, plan => plan.PlanId == repository.ProPlanId);
        Assert.DoesNotContain(plans, plan => plan.PlanId == repository.InactivePlanId);
        Assert.All(plans, plan =>
        {
            Assert.Equal("VND", plan.Currency);
            Assert.Equal(30, plan.DurationDays);
        });
    }

    [Fact]
    public async Task SubscribeAsync_WhenActivePatientAndPlanExists_CreatesPendingSubscription()
    {
        var repository = new FakeSubscriptionRepository();
        var service = new SubscriptionService(repository);

        var result = await service.SubscribeAsync(new SubscribeCommand(repository.PatientUserId, repository.ProPlanId));

        Assert.True(result.Succeeded);
        Assert.Equal("PendingPayment", result.Subscription!.Status);
        Assert.Equal(nameof(PaymentStatus.Pending), result.Subscription.PaymentStatus);
        Assert.Equal(99000m, result.Subscription.Price);
        Assert.Equal("VND", result.Subscription.Currency);
    }

    [Fact]
    public async Task SubscribeAsync_WhenPlanIsNotFound_ReturnsPlanNotFound()
    {
        var repository = new FakeSubscriptionRepository();
        var service = new SubscriptionService(repository);

        var result = await service.SubscribeAsync(new SubscribeCommand(repository.PatientUserId, Guid.NewGuid()));

        Assert.False(result.Succeeded);
        Assert.Equal(SubscriptionFailureReason.PlanNotFound, result.FailureReason);
    }

    [Fact]
    public async Task SubscribeAsync_WhenPlanIsInactive_ReturnsPlanNotFound()
    {
        var repository = new FakeSubscriptionRepository();
        var service = new SubscriptionService(repository);

        var result = await service.SubscribeAsync(new SubscribeCommand(repository.PatientUserId, repository.InactivePlanId));

        Assert.False(result.Succeeded);
        Assert.Equal(SubscriptionFailureReason.PlanNotFound, result.FailureReason);
    }

    [Fact]
    public async Task SubscribeAsync_WhenPlanIsDeleted_ReturnsPlanNotFound()
    {
        var repository = new FakeSubscriptionRepository();
        var service = new SubscriptionService(repository);

        var result = await service.SubscribeAsync(new SubscribeCommand(repository.PatientUserId, repository.DeletedPlanId));

        Assert.False(result.Succeeded);
        Assert.Equal(SubscriptionFailureReason.PlanNotFound, result.FailureReason);
    }

    [Fact]
    public async Task SubscribeAsync_WhenUserIsNotActivePatient_ReturnsAccessDenied()
    {
        var repository = new FakeSubscriptionRepository
        {
            HasPatientRole = false
        };
        var service = new SubscriptionService(repository);

        var result = await service.SubscribeAsync(new SubscribeCommand(repository.PatientUserId, repository.ProPlanId));

        Assert.False(result.Succeeded);
        Assert.Equal(SubscriptionFailureReason.AccessDenied, result.FailureReason);
    }

    [Fact]
    public async Task GetCurrentSubscriptionAsync_WhenSubscriptionExists_ReturnsOwnSubscription()
    {
        var repository = new FakeSubscriptionRepository();
        var service = new SubscriptionService(repository);
        var subscribe = await service.SubscribeAsync(new SubscribeCommand(repository.PatientUserId, repository.ProPlanId));

        var result = await service.GetCurrentSubscriptionAsync(repository.PatientUserId);

        Assert.True(result.Succeeded);
        Assert.Equal(subscribe.Subscription!.SubscriptionId, result.Subscription!.SubscriptionId);
    }

    [Fact]
    public async Task ConfirmPaymentAsync_WhenPendingSubscriptionExists_ActivatesSubscription()
    {
        var repository = new FakeSubscriptionRepository();
        var service = new SubscriptionService(repository);
        var subscribe = await service.SubscribeAsync(new SubscribeCommand(repository.PatientUserId, repository.ProPlanId));

        var result = await service.ConfirmPaymentAsync(
            repository.PatientUserId,
            subscribe.Subscription!.SubscriptionId);

        Assert.True(result.Succeeded);
        Assert.Equal(nameof(SubscriptionStatus.Active), result.Subscription!.Status);
        Assert.Equal(nameof(PaymentStatus.Paid), result.Subscription.PaymentStatus);
        Assert.Equal(repository.Now, result.Subscription.StartDate);
        Assert.Equal(repository.Now.AddDays(30), result.Subscription.EndDate);
    }

    [Fact]
    public async Task ConfirmPaymentAsync_WhenSubscriptionDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakeSubscriptionRepository();
        var service = new SubscriptionService(repository);

        var result = await service.ConfirmPaymentAsync(repository.PatientUserId, Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(SubscriptionFailureReason.SubscriptionNotFound, result.FailureReason);
    }

    [Fact]
    public async Task ConfirmPaymentAsync_WhenSubscriptionBelongsToAnotherUser_ReturnsNotFound()
    {
        var repository = new FakeSubscriptionRepository();
        var service = new SubscriptionService(repository);
        var otherSubscriptionId = repository.AddPendingSubscription(repository.OtherPatientUserId);

        var result = await service.ConfirmPaymentAsync(repository.PatientUserId, otherSubscriptionId);

        Assert.False(result.Succeeded);
        Assert.Equal(SubscriptionFailureReason.SubscriptionNotFound, result.FailureReason);
    }

    [Fact]
    public async Task ConfirmPaymentAsync_WhenAlreadyPaid_ReturnsConflictReason()
    {
        var repository = new FakeSubscriptionRepository();
        var service = new SubscriptionService(repository);
        var subscribe = await service.SubscribeAsync(new SubscribeCommand(repository.PatientUserId, repository.ProPlanId));
        await service.ConfirmPaymentAsync(repository.PatientUserId, subscribe.Subscription!.SubscriptionId);

        var result = await service.ConfirmPaymentAsync(repository.PatientUserId, subscribe.Subscription.SubscriptionId);

        Assert.False(result.Succeeded);
        Assert.Equal(SubscriptionFailureReason.SubscriptionAlreadyPaid, result.FailureReason);
    }

    private sealed class FakeSubscriptionRepository : ISubscriptionRepository
    {
        private readonly Dictionary<Guid, SubscriptionPlanRecord> plans = [];
        private readonly Dictionary<Guid, SubscriptionRecord> subscriptions = [];

        public FakeSubscriptionRepository()
        {
            plans[FreePlanId] = new SubscriptionPlanRecord(FreePlanId, "Free", "Free", 0m, true, false);
            plans[ProPlanId] = new SubscriptionPlanRecord(ProPlanId, "Pro", "Pro", 99000m, true, false);
            plans[InactivePlanId] = new SubscriptionPlanRecord(InactivePlanId, "Legacy", "Legacy", 50000m, false, false);
            plans[DeletedPlanId] = new SubscriptionPlanRecord(DeletedPlanId, "Deleted", "Deleted", 50000m, true, true);
        }

        public Guid PatientUserId { get; } = Guid.NewGuid();
        public Guid OtherPatientUserId { get; } = Guid.NewGuid();
        public Guid FreePlanId { get; } = Guid.NewGuid();
        public Guid ProPlanId { get; } = Guid.NewGuid();
        public Guid InactivePlanId { get; } = Guid.NewGuid();
        public Guid DeletedPlanId { get; } = Guid.NewGuid();
        public DateTimeOffset Now { get; } = new(2026, 5, 19, 12, 0, 0, TimeSpan.Zero);
        public bool HasPatientRole { get; set; } = true;
        public int UserStatus { get; set; } = (int)AccountStatus.Active;

        public Guid AddPendingSubscription(Guid userId)
        {
            var subscription = CreateRecord(userId, ProPlanId, SubscriptionStatus.Inactive, PaymentStatus.Pending, null, null);
            subscriptions[subscription.SubscriptionId] = subscription;

            return subscription.SubscriptionId;
        }

        public Task<IReadOnlyList<SubscriptionPlanRecord>> GetActivePlansAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<SubscriptionPlanRecord>>(
                plans.Values
                    .Where(plan => plan.IsActive && !plan.IsDeleted)
                    .ToList());
        }

        public Task<SubscriptionUserState?> GetUserStateAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            if (userId != PatientUserId && userId != OtherPatientUserId)
            {
                return Task.FromResult<SubscriptionUserState?>(null);
            }

            return Task.FromResult<SubscriptionUserState?>(new SubscriptionUserState(
                userId,
                UserStatus,
                HasPatientRole));
        }

        public Task<SubscriptionPlanRecord?> GetPlanByIdAsync(
            Guid planId,
            CancellationToken cancellationToken = default)
        {
            plans.TryGetValue(planId, out var plan);

            return Task.FromResult(plan);
        }

        public Task<bool> HasCurrentSubscriptionAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(subscriptions.Values.Any(subscription =>
                subscription.UserId == userId &&
                subscription.Status is
                    SubscriptionStatus.Active or
                    SubscriptionStatus.PastDue or
                    SubscriptionStatus.Cancelled));
        }

        public Task<SubscriptionRecord?> GetCurrentSubscriptionAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(subscriptions.Values
                .Where(subscription => subscription.UserId == userId)
                .OrderByDescending(subscription => subscription.Status == SubscriptionStatus.Active)
                .FirstOrDefault());
        }

        public Task<SubscriptionRecord> CreatePendingSubscriptionAsync(
            CreateSubscriptionDraft draft,
            CancellationToken cancellationToken = default)
        {
            var subscription = CreateRecord(
                draft.UserId,
                draft.PlanId,
                SubscriptionStatus.Inactive,
                PaymentStatus.Pending,
                null,
                null);
            subscriptions[subscription.SubscriptionId] = subscription;

            return Task.FromResult(subscription);
        }

        public Task<SubscriptionRepositoryResult> ConfirmPaymentPlaceholderAsync(
            Guid userId,
            Guid subscriptionId,
            int durationDays,
            CancellationToken cancellationToken = default)
        {
            if (!subscriptions.TryGetValue(subscriptionId, out var subscription) ||
                subscription.UserId != userId)
            {
                return Task.FromResult(new SubscriptionRepositoryResult(
                    false,
                    null,
                    SubscriptionFailureReason.SubscriptionNotFound,
                    "Subscription was not found."));
            }

            if (subscription.Status == SubscriptionStatus.Active ||
                subscription.PaymentStatus == PaymentStatus.Paid)
            {
                return Task.FromResult(new SubscriptionRepositoryResult(
                    false,
                    null,
                    SubscriptionFailureReason.SubscriptionAlreadyPaid,
                    "Subscription payment has already been confirmed."));
            }

            var updated = subscription with
            {
                Status = SubscriptionStatus.Active,
                PaymentStatus = PaymentStatus.Paid,
                PaidAt = Now,
                CurrentPeriodEnd = Now.AddDays(durationDays)
            };
            subscriptions[subscriptionId] = updated;

            return Task.FromResult(new SubscriptionRepositoryResult(true, updated, null, null));
        }

        private SubscriptionRecord CreateRecord(
            Guid userId,
            Guid planId,
            SubscriptionStatus status,
            PaymentStatus paymentStatus,
            DateTimeOffset? paidAt,
            DateTimeOffset? currentPeriodEnd)
        {
            var plan = plans[planId];

            return new SubscriptionRecord(
                Guid.NewGuid(),
                userId,
                plan.PlanId,
                plan.Name,
                status,
                paymentStatus,
                paidAt,
                currentPeriodEnd,
                plan.Price,
                "VND");
        }
    }
}
