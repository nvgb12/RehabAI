using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RehabAI.Api.Authorization;

namespace RehabAI.Api.Controllers;

[ApiController]
[Route("api")]
public class EngagementController : ControllerBase
{
    [Authorize(Policy = AccessPolicies.ActivePatient)]
    [HttpPost("subscriptions/pro")]
    public IActionResult SubscribeToPro() => Accepted(new { message = "UC-24 scaffolded: subscribe to Pro." });

    [Authorize(Policy = AccessPolicies.ActivePatient)]
    [HttpGet("subscriptions/scaffold/me")]
    public IActionResult ManageSubscription() => Ok(new { message = "UC-25 scaffolded: manage subscription." });

    [Authorize(Policy = AccessPolicies.ActivePatient)]
    [HttpPost("disputes")]
    public IActionResult ManageDisputes() => Accepted(new { message = "UC-28 scaffolded: submit dispute/no-show record." });

    [Authorize(Policy = AccessPolicies.ActivePatient)]
    [HttpPost("reviews")]
    public IActionResult SubmitReview() => Accepted(new { message = "UC-29 scaffolded: submit review." });
}
