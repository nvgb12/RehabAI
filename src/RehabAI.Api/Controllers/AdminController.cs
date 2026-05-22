using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RehabAI.Api.Authorization;
using RehabAI.Api.Contracts.Doctors;
using RehabAI.Api.Contracts.MedicalServices;
using RehabAI.Api.Contracts.Orders;
using RehabAI.Api.Contracts.Products;
using RehabAI.Application.Doctors;
using RehabAI.Application.MedicalServices;
using RehabAI.Application.Orders;
using RehabAI.Application.Products;
using RehabAI.Application.Reports;

namespace RehabAI.Api.Controllers;

[ApiController]
[Authorize(Policy = AccessPolicies.ActiveAdmin)]
[Route("api/admin")]
public class AdminController(
    IDoctorService doctorService,
    IMedicalServiceManager medicalServiceManager,
    IProductManager productManager,
    IOrderService orderService,
    IRevenueReportService revenueReportService,
    IHostEnvironment hostEnvironment) : ControllerBase
{
    [HttpGet("doctors")]
    public async Task<IActionResult> GetDoctors(CancellationToken cancellationToken)
    {
        var doctors = await doctorService.GetAdminDoctorsAsync(cancellationToken);

        return Ok(doctors);
    }

    [HttpGet("doctors/{doctorProfileId:guid}")]
    public async Task<IActionResult> GetDoctor(Guid doctorProfileId, CancellationToken cancellationToken)
    {
        var doctor = await doctorService.GetAdminDoctorByIdAsync(doctorProfileId, cancellationToken);

        return doctor is null
            ? NotFound(new { message = "Doctor profile was not found." })
            : Ok(doctor);
    }

    [HttpPost("doctors")]
    public async Task<IActionResult> CreateDoctor(CreateDoctorRequest request, CancellationToken cancellationToken)
    {
        var result = await doctorService.CreateDoctorAsync(
            new CreateDoctorCommand(
                request.FullName,
                request.Email,
                request.PhoneNumber,
                request.SpecialtyId,
                request.Bio,
                request.YearsOfExperience),
            cancellationToken);

        if (!result.Succeeded)
        {
            return result.FailureReason switch
            {
                CreateDoctorFailureReason.DuplicateEmail => Conflict(new { result.Message }),
                CreateDoctorFailureReason.MissingDoctorRole => StatusCode(StatusCodes.Status500InternalServerError, new { result.Message }),
                CreateDoctorFailureReason.SpecialtyNotFound => BadRequest(new { result.Message }),
                CreateDoctorFailureReason.EmailDeliveryFailed => StatusCode(
                    StatusCodes.Status502BadGateway,
                    BuildCreateDoctorResponse(result)),
                _ => BadRequest(new { result.Message })
            };
        }

        return Ok(BuildCreateDoctorResponse(result));
    }

    [HttpPost("doctors/{doctorProfileId:guid}/public-profile/approve")]
    public async Task<IActionResult> ApproveDoctorPublicProfile(
        Guid doctorProfileId,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        var result = await doctorService.ApprovePublicProfileAsync(
            doctorProfileId,
            currentUserId.Value,
            cancellationToken);

        return result.Succeeded
            ? Ok(new { message = result.Message, doctor = result.Doctor })
            : ToDoctorPublicProfileReviewErrorResponse(result);
    }

    [HttpPost("doctors/{doctorProfileId:guid}/public-profile/reject")]
    public async Task<IActionResult> RejectDoctorPublicProfile(
        Guid doctorProfileId,
        RejectDoctorPublicProfileRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized(new { message = "Authenticated user is required." });
        }

        var result = await doctorService.RejectPublicProfileAsync(
            doctorProfileId,
            currentUserId.Value,
            request.RejectionReason,
            cancellationToken);

        return result.Succeeded
            ? Ok(new { message = result.Message, doctor = result.Doctor })
            : ToDoctorPublicProfileReviewErrorResponse(result);
    }

    [HttpPost("medical-services")]
    public async Task<IActionResult> CreateMedicalService(
        CreateMedicalServiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await medicalServiceManager.CreateAsync(ToCreateCommand(request), cancellationToken);

        if (!result.Succeeded)
        {
            return ToMedicalServiceErrorResponse(result);
        }

        return CreatedAtAction(
            nameof(MedicalServicesController.GetMedicalService),
            "MedicalServices",
            new { id = result.MedicalService!.Id },
            new
            {
                message = result.Message,
                medicalService = result.MedicalService
            });
    }

    [HttpPut("medical-services/{id:guid}")]
    public async Task<IActionResult> UpdateMedicalService(
        Guid id,
        UpsertMedicalServiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await medicalServiceManager.UpdateAsync(id, ToCommand(request), cancellationToken);

        if (!result.Succeeded)
        {
            return ToMedicalServiceErrorResponse(result);
        }

        return Ok(new
        {
            message = result.Message,
            medicalService = result.MedicalService
        });
    }

    [HttpDelete("medical-services/{id:guid}")]
    public async Task<IActionResult> DeleteMedicalService(Guid id, CancellationToken cancellationToken)
    {
        var result = await medicalServiceManager.SoftDeleteAsync(id, cancellationToken);

        if (!result.Succeeded)
        {
            return ToMedicalServiceErrorResponse(result);
        }

        return Ok(new { message = result.Message });
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(CancellationToken cancellationToken)
    {
        var products = await productManager.GetProductsAsync(cancellationToken);

        return Ok(products);
    }

    [HttpGet("products/{productId:guid}")]
    public async Task<IActionResult> GetProduct(Guid productId, CancellationToken cancellationToken)
    {
        var product = await productManager.GetProductByIdAsync(productId, cancellationToken);

        return product is null
            ? NotFound(new { message = "Product was not found." })
            : Ok(product);
    }

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productManager.CreateAsync(ToCreateProductCommand(request), cancellationToken);

        if (!result.Succeeded)
        {
            return ToProductErrorResponse(result);
        }

        return CreatedAtAction(
            nameof(GetProduct),
            new { productId = result.Product!.Id },
            new
            {
                message = result.Message,
                product = result.Product
            });
    }

    [HttpPut("products/{productId:guid}")]
    public async Task<IActionResult> UpdateProduct(
        Guid productId,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productManager.UpdateAsync(productId, ToProductCommand(request), cancellationToken);

        if (!result.Succeeded)
        {
            return ToProductErrorResponse(result);
        }

        return Ok(new
        {
            message = result.Message,
            product = result.Product
        });
    }

    [HttpDelete("products/{productId:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid productId, CancellationToken cancellationToken)
    {
        var result = await productManager.SoftDeleteAsync(productId, cancellationToken);

        if (!result.Succeeded)
        {
            return ToProductErrorResponse(result);
        }

        return Ok(new { message = result.Message });
    }

    [HttpGet("users")]
    public IActionResult ManageUsers() => Ok(Array.Empty<object>());

    [HttpGet("reports")]
    public IActionResult ViewReports() => Ok(new { message = "UC-19 scaffolded: reports." });

    [HttpGet("reports/revenue")]
    public async Task<IActionResult> GetRevenueReport(
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        CancellationToken cancellationToken)
    {
        var result = await revenueReportService.GetRevenueReportAsync(
            new RevenueReportQuery(fromDate, toDate),
            cancellationToken);

        return result.Succeeded
            ? Ok(result.Report)
            : ToRevenueReportErrorResponse(result);
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(
        [FromQuery] string? status,
        [FromQuery] string? paymentStatus,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        CancellationToken cancellationToken)
    {
        var result = await orderService.GetAdminOrdersAsync(
            new AdminOrderQuery(status, paymentStatus, fromDate, toDate),
            cancellationToken);

        return result.Succeeded
            ? Ok(result.Orders)
            : ToAdminOrderListErrorResponse(result);
    }

    [HttpGet("orders/{orderId:guid}")]
    public async Task<IActionResult> GetOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await orderService.GetAdminOrderByIdAsync(orderId, cancellationToken);

        return order is null
            ? NotFound(new { message = "Order was not found." })
            : Ok(order);
    }

    [HttpPut("orders/{orderId:guid}/status")]
    public async Task<IActionResult> UpdateOrderStatus(
        Guid orderId,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await orderService.UpdateAdminOrderStatusAsync(
            orderId,
            new UpdateOrderStatusCommand(request.Status),
            cancellationToken);

        if (!result.Succeeded)
        {
            return ToAdminOrderErrorResponse(result);
        }

        return Ok(new
        {
            message = result.Message,
            order = result.Order
        });
    }

    [HttpGet("subscriptions")]
    public IActionResult ManageSubscriptionsAdmin() => Ok(new { message = "UC-27 scaffolded: admin subscription management." });

    [HttpGet("payouts")]
    public IActionResult ManagePayouts() => Ok(new { message = "UC-30 scaffolded: payout management." });

    private object BuildCreateDoctorResponse(CreateDoctorResult result)
    {
        var response = new Dictionary<string, object?>
        {
            ["message"] = result.Message,
            ["userId"] = result.UserId,
            ["doctorProfileId"] = result.DoctorProfileId,
            ["email"] = result.Email
        };

        if (hostEnvironment.IsDevelopment())
        {
            response["invitationToken"] = result.InvitationToken;
            response["passwordSetupUrl"] = result.PasswordSetupUrl;
        }

        return response;
    }

    private static UpsertMedicalServiceCommand ToCommand(UpsertMedicalServiceRequest request)
    {
        return new UpsertMedicalServiceCommand(
            request.Name,
            request.Description,
            request.DurationMinutes,
            request.Price,
            request.Currency,
            request.IsActive,
            request.NoShowFeeEnabled,
            request.NoShowFeeAmount);
    }

    private static UpsertMedicalServiceCommand ToCreateCommand(CreateMedicalServiceRequest request)
    {
        return new UpsertMedicalServiceCommand(
            request.Name,
            request.Description,
            request.DurationMinutes,
            request.Price,
            request.Currency,
            request.IsActive ?? true,
            request.NoShowFeeEnabled,
            request.NoShowFeeAmount);
    }

    private static UpsertProductCommand ToCreateProductCommand(CreateProductRequest request)
    {
        return new UpsertProductCommand(
            request.Name,
            request.Description,
            request.CategoryId,
            request.Price,
            request.Currency,
            request.StockQuantity,
            request.ImageUrl,
            request.IsActive ?? true);
    }

    private static UpsertProductCommand ToProductCommand(UpdateProductRequest request)
    {
        return new UpsertProductCommand(
            request.Name,
            request.Description,
            request.CategoryId,
            request.Price,
            request.Currency,
            request.StockQuantity,
            request.ImageUrl,
            request.IsActive);
    }

    private IActionResult ToMedicalServiceErrorResponse(MedicalServiceResult result)
    {
        return result.FailureReason switch
        {
            MedicalServiceFailureReason.NotFound => NotFound(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }

    private IActionResult ToProductErrorResponse(ProductResult result)
    {
        return result.FailureReason switch
        {
            ProductFailureReason.NotFound => NotFound(new { message = result.Message }),
            ProductFailureReason.DuplicateSlug => Conflict(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }

    private IActionResult ToAdminOrderListErrorResponse(AdminOrderListResult result)
    {
        return result.FailureReason switch
        {
            OrderFailureReason.InvalidStatus => BadRequest(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }

    private IActionResult ToAdminOrderErrorResponse(AdminOrderResult result)
    {
        return result.FailureReason switch
        {
            OrderFailureReason.OrderNotFound => NotFound(new { message = result.Message }),
            OrderFailureReason.InvalidStatusTransition => Conflict(new { message = result.Message }),
            OrderFailureReason.InvalidStatus => BadRequest(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }

    private IActionResult ToRevenueReportErrorResponse(RevenueReportResult result)
    {
        return result.FailureReason switch
        {
            RevenueReportFailureReason.Validation => BadRequest(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }

    private IActionResult ToDoctorPublicProfileReviewErrorResponse(AdminDoctorPublicProfileReviewResult result)
    {
        return result.FailureReason switch
        {
            AdminDoctorPublicProfileReviewFailureReason.DoctorNotFound => NotFound(new { message = result.Message }),
            AdminDoctorPublicProfileReviewFailureReason.InvalidStatus => Conflict(new { message = result.Message, doctor = result.Doctor }),
            _ => BadRequest(new { message = result.Message })
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
