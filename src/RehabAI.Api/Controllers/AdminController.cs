using Microsoft.AspNetCore.Mvc;

namespace RehabAI.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    [HttpGet("users")]
    public IActionResult ManageUsers() => Ok(Array.Empty<object>());

    [HttpGet("reports")]
    public IActionResult ViewReports() => Ok(new { message = "UC-19 scaffolded: reports." });

    [HttpGet("orders")]
    public IActionResult ManageOrdersAdmin() => Ok(new { message = "UC-23 scaffolded: admin order management." });

    [HttpGet("subscriptions")]
    public IActionResult ManageSubscriptionsAdmin() => Ok(new { message = "UC-27 scaffolded: admin subscription management." });

    [HttpGet("payouts")]
    public IActionResult ManagePayouts() => Ok(new { message = "UC-30 scaffolded: payout management." });
}
