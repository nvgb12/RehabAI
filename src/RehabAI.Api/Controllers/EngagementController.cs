using Microsoft.AspNetCore.Mvc;

namespace RehabAI.Api.Controllers;

[ApiController]
[Route("api")]
public class EngagementController : ControllerBase
{
    [HttpPost("subscriptions/pro")]
    public IActionResult SubscribeToPro() => Accepted(new { message = "UC-24 scaffolded: subscribe to Pro." });

    [HttpGet("subscriptions/me")]
    public IActionResult ManageSubscription() => Ok(new { message = "UC-25 scaffolded: manage subscription." });

    [HttpPost("disputes")]
    public IActionResult ManageDisputes() => Accepted(new { message = "UC-28 scaffolded: submit dispute/no-show record." });

    [HttpPost("reviews")]
    public IActionResult SubmitReview() => Accepted(new { message = "UC-29 scaffolded: submit review." });
}
