using Microsoft.AspNetCore.Mvc;

namespace RehabAI.Api.Controllers;

[ApiController]
[Route("api")]
public class CommerceController : ControllerBase
{
    [HttpGet("products")]
    public IActionResult BrowseProducts() => Ok(Array.Empty<object>());

    [HttpGet("cart")]
    public IActionResult GetCart() => Ok(new { items = Array.Empty<object>() });

    [HttpPost("orders")]
    public IActionResult PlaceOrder() => Accepted(new { message = "UC-12 scaffolded: place order." });

    [HttpGet("orders")]
    public IActionResult ManageOrders() => Ok(Array.Empty<object>());

    [HttpPost("payments/create-session")]
    public IActionResult CreatePaymentSession() => Accepted(new { message = "UC-13 scaffolded: create payment session." });

    [HttpPost("payments/webhook")]
    public IActionResult PaymentWebhook() => Ok(new { received = true });
}
