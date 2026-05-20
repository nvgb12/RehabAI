using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RehabAI.Api.Contracts.Subscriptions;
using RehabAI.Api.Controllers;
using RehabAI.Application.Subscriptions;
using RehabAI.Domain.Enums;

namespace RehabAI.UnitTests.Subscriptions;

public class SubscriptionsControllerTests
{
    [Fact]
    public async Task GetSubscriptionPlans_ReturnsPlans()
    {
        var planId = Guid.NewGuid();
        var controller = CreateController(new FakeSubscriptionService
        {
            Plans =
            [
                new SubscriptionPlanResponse(
                    planId,
                    "Pro",
                    "Pro subscription plan.",
                    99000m,
                    "VND",
                    30,
                    true)
            ]
        });

        var response = await controller.GetSubscriptionPlans(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response);
        var plans = Assert.IsAssignableFrom<IReadOnlyList<SubscriptionPlanResponse>>(ok.Value);
        Assert.Equal(planId, Assert.Single(plans).PlanId);
    }

    [Fact]
    public async Task Subscribe_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var controller = CreateController(new FakeSubscriptionService());

        var response = await controller.Subscribe(new SubscribeRequest(Guid.NewGuid()), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(response);
    }

    [Fact]
    public async Task Subscribe_WhenUserIsNotActivePatient_ReturnsForbidden()
    {
        var controller = CreateController(new FakeSubscriptionService
        {
            SubscribeResult = new SubscriptionResult(
                false,
                "Only active Patient users can manage subscriptions.",
                FailureReason: SubscriptionFailureReason.AccessDenied)
        }, Guid.NewGuid());

        var response = await controller.Subscribe(new SubscribeRequest(Guid.NewGuid()), CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Subscribe_WhenActivePatient_ReturnsCreatedSubscription()
    {
        var userId = Guid.NewGuid();
        var subscription = ActiveSubscription(Guid.NewGuid(), PaymentStatus.Pending, "PendingPayment");
        var controller = CreateController(new FakeSubscriptionService
        {
            SubscribeResult = new SubscriptionResult(
                true,
                "Subscription created and is pending payment.",
                subscription)
        }, userId);

        var response = await controller.Subscribe(
            new SubscribeRequest(subscription.PlanId),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(response);
        Assert.Equal(nameof(SubscriptionsController.GetMySubscription), created.ActionName);
    }

    [Fact]
    public async Task GetMySubscription_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var controller = CreateController(new FakeSubscriptionService());

        var response = await controller.GetMySubscription(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(response);
    }

    [Fact]
    public async Task ConfirmSubscriptionPayment_WhenSubscriptionIsNotFound_ReturnsNotFound()
    {
        var controller = CreateController(new FakeSubscriptionService
        {
            ConfirmResult = new SubscriptionResult(
                false,
                "Subscription was not found.",
                FailureReason: SubscriptionFailureReason.SubscriptionNotFound)
        }, Guid.NewGuid());

        var response = await controller.ConfirmSubscriptionPayment(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(response);
    }

    [Fact]
    public async Task ConfirmSubscriptionPayment_WhenAlreadyPaid_ReturnsConflict()
    {
        var subscription = ActiveSubscription(Guid.NewGuid(), PaymentStatus.Paid, nameof(SubscriptionStatus.Active));
        var controller = CreateController(new FakeSubscriptionService
        {
            ConfirmResult = new SubscriptionResult(
                false,
                "Subscription payment has already been confirmed.",
                subscription,
                SubscriptionFailureReason.SubscriptionAlreadyPaid)
        }, Guid.NewGuid());

        var response = await controller.ConfirmSubscriptionPayment(
            subscription.SubscriptionId,
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(response);
    }

    private static SubscriptionsController CreateController(
        FakeSubscriptionService subscriptionService,
        Guid? userId = null)
    {
        var controller = new SubscriptionsController(subscriptionService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        if (userId.HasValue)
        {
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", userId.Value.ToString())],
                "TestAuth"));
        }

        return controller;
    }

    private static SubscriptionResponse ActiveSubscription(
        Guid subscriptionId,
        PaymentStatus paymentStatus,
        string status)
    {
        return new SubscriptionResponse(
            subscriptionId,
            Guid.NewGuid(),
            "Pro",
            status,
            paymentStatus.ToString(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(30),
            99000m,
            "VND");
    }

    private sealed class FakeSubscriptionService : ISubscriptionService
    {
        public IReadOnlyList<SubscriptionPlanResponse> Plans { get; set; } = [];
        public CurrentSubscriptionResult CurrentResult { get; set; } = new(
            true,
            "No current subscription was found.");
        public SubscriptionResult SubscribeResult { get; set; } = new(
            false,
            "Not used.",
            FailureReason: SubscriptionFailureReason.Validation);
        public SubscriptionResult ConfirmResult { get; set; } = new(
            false,
            "Not used.",
            FailureReason: SubscriptionFailureReason.Validation);

        public Task<IReadOnlyList<SubscriptionPlanResponse>> GetActivePlansAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Plans);
        }

        public Task<CurrentSubscriptionResult> GetCurrentSubscriptionAsync(
            Guid currentUserId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CurrentResult);
        }

        public Task<SubscriptionResult> SubscribeAsync(
            SubscribeCommand command,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SubscribeResult);
        }

        public Task<SubscriptionResult> ConfirmPaymentAsync(
            Guid currentUserId,
            Guid subscriptionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ConfirmResult);
        }
    }
}
