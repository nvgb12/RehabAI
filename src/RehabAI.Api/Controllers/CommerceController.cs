using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RehabAI.Api.Authorization;
using RehabAI.Api.Contracts.Orders;
using RehabAI.Application.Orders;
using RehabAI.Application.Products;

namespace RehabAI.Api.Controllers;

[ApiController]
[Route("api")]
public class CommerceController(
    IProductManager productManager,
    IOrderService orderService,
    IEndpointAccessService accessService) : ControllerBase
{
    [HttpGet("products")]
    public async Task<IActionResult> BrowseProducts(
        [FromQuery] string? keyword,
        [FromQuery] Guid? categoryId,
        CancellationToken cancellationToken)
    {
        var products = await productManager.GetPublicProductsAsync(
            new PublicProductQuery(keyword, categoryId),
            cancellationToken);

        return Ok(products);
    }

    [HttpGet("products/{productId:guid}")]
    public async Task<IActionResult> GetProduct(Guid productId, CancellationToken cancellationToken)
    {
        var product = await productManager.GetPublicProductByIdAsync(productId, cancellationToken);

        return product is null
            ? NotFound(new { message = "Product was not found." })
            : Ok(product);
    }

    [Authorize(Policy = AccessPolicies.ActivePatient)]
    [HttpGet("cart")]
    public IActionResult GetCart() => Ok(new { items = Array.Empty<object>() });

    [Authorize(Policy = AccessPolicies.ActivePatient)]
    [HttpPost("orders")]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        if (!await accessService.PatientProfileBelongsToUserAsync(
            currentUserId.Value,
            request.PatientProfileId,
            cancellationToken))
        {
            return Forbid();
        }

        var result = await orderService.CreateAsync(ToCommand(request), cancellationToken);

        if (!result.Succeeded)
        {
            return ToOrderErrorResponse(result);
        }

        return CreatedAtAction(
            nameof(GetOrder),
            new { orderId = result.Order!.OrderId },
            new
            {
                message = result.Message,
                order = result.Order
            });
    }

    [Authorize(Policy = AccessPolicies.ActivePatient)]
    [HttpGet("orders/my-orders")]
    public async Task<IActionResult> GetMyOrders(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        var result = await orderService.GetMyOrdersAsync(currentUserId.Value, cancellationToken);

        return result.Succeeded
            ? Ok(result.Orders)
            : ToCustomerOrderErrorResponse(result.FailureReason, result.Message);
    }

    [Authorize(Policy = AccessPolicies.ActivePatient)]
    [HttpGet("orders/my-orders/{orderId:guid}")]
    public async Task<IActionResult> GetMyOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        var result = await orderService.GetMyOrderByIdAsync(currentUserId.Value, orderId, cancellationToken);

        return result.Succeeded
            ? Ok(result.Order)
            : ToCustomerOrderErrorResponse(result.FailureReason, result.Message);
    }

    [Authorize(Policy = AccessPolicies.ActivePatient)]
    [HttpGet("orders/{orderId:guid}")]
    public async Task<IActionResult> GetOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        if (!await accessService.OrderBelongsToUserAsync(currentUserId.Value, orderId, cancellationToken))
        {
            return NotFound(new { message = "Order was not found." });
        }

        var order = await orderService.GetByIdAsync(orderId, cancellationToken);

        return order is null
            ? NotFound(new { message = "Order was not found." })
            : Ok(order);
    }

    [Authorize(Policy = AccessPolicies.ActivePatient)]
    [HttpPost("orders/{orderId:guid}/confirm-payment")]
    public async Task<IActionResult> ConfirmOrderPayment(Guid orderId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        if (!await accessService.OrderBelongsToUserAsync(currentUserId.Value, orderId, cancellationToken))
        {
            return NotFound(new { message = "Order was not found." });
        }

        var result = await orderService.ConfirmPaymentAsync(orderId, cancellationToken);

        if (!result.Succeeded)
        {
            return ToOrderErrorResponse(result);
        }

        return Ok(new
        {
            message = result.Message,
            order = result.Order
        });
    }

    [Authorize(Policy = AccessPolicies.ActivePatient)]
    [HttpGet("patients/{patientProfileId:guid}/orders")]
    public async Task<IActionResult> GetPatientOrders(Guid patientProfileId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        if (!await accessService.PatientProfileBelongsToUserAsync(currentUserId.Value, patientProfileId, cancellationToken))
        {
            return Forbid();
        }

        var orders = await orderService.GetPatientOrdersAsync(patientProfileId, cancellationToken);

        return Ok(orders);
    }

    [Authorize(Policy = AccessPolicies.ActivePatient)]
    [HttpPost("payments/create-session")]
    public IActionResult CreatePaymentSession() => Accepted(new { message = "UC-13 scaffolded: create payment session." });

    [HttpPost("payments/webhook")]
    public IActionResult PaymentWebhook() => Ok(new { received = true });

    private static CreateOrderCommand ToCommand(CreateOrderRequest request)
    {
        return new CreateOrderCommand(
            request.PatientProfileId,
            request.Items?.Select(item => new CreateOrderItemCommand(item.ProductId, item.Quantity)).ToList() ?? [],
            request.ShippingAddress);
    }

    private IActionResult ToOrderErrorResponse(OrderResult result)
    {
        return result.FailureReason switch
        {
            OrderFailureReason.PatientNotFound => NotFound(new { message = result.Message }),
            OrderFailureReason.ProductNotFound => NotFound(new { message = result.Message }),
            OrderFailureReason.ProductUnavailable => Conflict(new { message = result.Message }),
            OrderFailureReason.QuantityExceedsStock => Conflict(new { message = result.Message }),
            OrderFailureReason.OrderNotFound => NotFound(new { message = result.Message }),
            OrderFailureReason.OrderPaymentNotPending => Conflict(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }

    private IActionResult ToCustomerOrderErrorResponse(OrderFailureReason? failureReason, string message)
    {
        return failureReason switch
        {
            OrderFailureReason.OrderNotFound => NotFound(new { message }),
            OrderFailureReason.AccessDenied => StatusCode(StatusCodes.Status403Forbidden, new { message }),
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
