using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RehabAI.Api.Authorization;
using RehabAI.Api.Contracts.Subscriptions;
using RehabAI.Application.Subscriptions;

namespace RehabAI.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class SubscriptionsController(ISubscriptionService subscriptionService) : ControllerBase
{
    [HttpGet("subscription-plans")]
    public async Task<IActionResult> GetSubscriptionPlans(CancellationToken cancellationToken)
    {
        var plans = await subscriptionService.GetActivePlansAsync(cancellationToken);

        return Ok(plans);
    }

    [Authorize(Policy = AccessPolicies.ActivePatient)]
    [HttpGet("subscriptions/me")]
    public async Task<IActionResult> GetMySubscription(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        var result = await subscriptionService.GetCurrentSubscriptionAsync(
            currentUserId.Value,
            cancellationToken);

        return result.Succeeded
            ? Ok(new { subscription = result.Subscription })
            : ToSubscriptionErrorResponse(result.FailureReason, result.Message);
    }

    [Authorize(Policy = AccessPolicies.ActivePatient)]
    [HttpPost("subscriptions/subscribe")]
    public async Task<IActionResult> Subscribe(
        [FromBody] SubscribeRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        var result = await subscriptionService.SubscribeAsync(
            new SubscribeCommand(currentUserId.Value, request.PlanId),
            cancellationToken);

        if (!result.Succeeded)
        {
            return ToSubscriptionErrorResponse(result.FailureReason, result.Message);
        }

        return CreatedAtAction(
            nameof(GetMySubscription),
            routeValues: null,
            value: new
            {
                message = result.Message,
                subscription = result.Subscription
            });
    }

    [Authorize(Policy = AccessPolicies.ActivePatient)]
    [HttpPost("subscriptions/{subscriptionId:guid}/confirm-payment")]
    public async Task<IActionResult> ConfirmSubscriptionPayment(
        Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        var result = await subscriptionService.ConfirmPaymentAsync(
            currentUserId.Value,
            subscriptionId,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ToSubscriptionErrorResponse(result.FailureReason, result.Message);
        }

        return Ok(new
        {
            message = result.Message,
            subscription = result.Subscription
        });
    }

    private IActionResult ToSubscriptionErrorResponse(
        SubscriptionFailureReason? failureReason,
        string message)
    {
        return failureReason switch
        {
            SubscriptionFailureReason.AccessDenied => StatusCode(StatusCodes.Status403Forbidden, new { message }),
            SubscriptionFailureReason.PlanNotFound => NotFound(new { message }),
            SubscriptionFailureReason.SubscriptionNotFound => NotFound(new { message }),
            SubscriptionFailureReason.SubscriptionAlreadyPaid => Conflict(new { message }),
            SubscriptionFailureReason.CurrentSubscriptionExists => Conflict(new { message }),
            _ => BadRequest(new { message })
        };
    }

    private Guid? GetCurrentUserId()
    {
        var claimValue =
            User.FindFirstValue("sub") ??
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }
}
