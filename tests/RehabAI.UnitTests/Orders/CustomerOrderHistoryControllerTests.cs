using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RehabAI.Api.Authorization;
using RehabAI.Api.Controllers;
using RehabAI.Application.Orders;
using RehabAI.Application.Products;
using RehabAI.Domain.Enums;

namespace RehabAI.UnitTests.Orders;

public class CustomerOrderHistoryControllerTests
{
    [Fact]
    public async Task GetMyOrders_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var controller = CreateController(new FakeOrderService());

        var response = await controller.GetMyOrders(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(response);
    }

    [Fact]
    public async Task GetMyOrders_WhenUserIsNotActivePatient_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var controller = CreateController(new FakeOrderService
        {
            MyOrdersResult = new CustomerOrderListResult(
                false,
                "Only active Patient users can view customer orders.",
                [],
                OrderFailureReason.AccessDenied)
        }, userId);

        var response = await controller.GetMyOrders(CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task GetMyOrders_WhenUserIsActivePatient_ReturnsOwnOrders()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var controller = CreateController(new FakeOrderService
        {
            MyOrdersResult = new CustomerOrderListResult(
                true,
                "Orders retrieved successfully.",
                [
                    new CustomerOrderSummaryResponse(
                        orderId,
                        "ORD-001",
                        DateTimeOffset.UtcNow,
                        200000m,
                        "VND",
                        nameof(OrderStatus.Processing),
                        nameof(PaymentStatus.Paid))
                ])
        }, userId);

        var response = await controller.GetMyOrders(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response);
        var orders = Assert.IsAssignableFrom<IReadOnlyList<CustomerOrderSummaryResponse>>(ok.Value);
        Assert.Equal(orderId, Assert.Single(orders).OrderId);
    }

    [Fact]
    public async Task GetMyOrder_WhenOrderBelongsToPatient_ReturnsOrderDetail()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var controller = CreateController(new FakeOrderService
        {
            MyOrderDetailResult = new CustomerOrderDetailResult(
                true,
                "Order retrieved successfully.",
                new CustomerOrderDetailResponse(
                    orderId,
                    "ORD-001",
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    "Stroke rehabilitation home address",
                    200000m,
                    "VND",
                    nameof(OrderStatus.Processing),
                    nameof(PaymentStatus.Paid),
                    [
                        new CustomerOrderItemResponse(
                            Guid.NewGuid(),
                            Guid.NewGuid(),
                            "Stroke Recovery Therapy Ball",
                            2,
                            100000m,
                            200000m)
                    ]))
        }, userId);

        var response = await controller.GetMyOrder(orderId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response);
        var order = Assert.IsType<CustomerOrderDetailResponse>(ok.Value);
        Assert.Equal(orderId, order.OrderId);
    }

    [Fact]
    public async Task GetMyOrder_WhenOrderDoesNotBelongToPatient_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var controller = CreateController(new FakeOrderService
        {
            MyOrderDetailResult = new CustomerOrderDetailResult(
                false,
                "Order was not found.",
                FailureReason: OrderFailureReason.OrderNotFound)
        }, userId);

        var response = await controller.GetMyOrder(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(response);
    }

    private static CommerceController CreateController(FakeOrderService orderService, Guid? userId = null)
    {
        var controller = new CommerceController(new FakeProductManager(), orderService, new FakeEndpointAccessService())
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

    private sealed class FakeProductManager : IProductManager
    {
        public Task<IReadOnlyList<ProductResponse>> GetProductsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ProductResponse>>([]);
        }

        public Task<ProductResponse?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ProductResponse?>(null);
        }

        public Task<IReadOnlyList<PublicProductResponse>> GetPublicProductsAsync(
            PublicProductQuery query,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PublicProductResponse>>([]);
        }

        public Task<PublicProductResponse?> GetPublicProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PublicProductResponse?>(null);
        }

        public Task<ProductResult> CreateAsync(UpsertProductCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ProductResult(false, "Not used."));
        }

        public Task<ProductResult> UpdateAsync(Guid id, UpsertProductCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ProductResult(false, "Not used."));
        }

        public Task<ProductResult> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ProductResult(false, "Not used."));
        }
    }

    private sealed class FakeOrderService : IOrderService
    {
        public CustomerOrderListResult MyOrdersResult { get; set; } = new(true, "Not used.", []);
        public CustomerOrderDetailResult MyOrderDetailResult { get; set; } = new(false, "Not used.");

        public Task<OrderResult> CreateAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new OrderResult(false, "Not used."));
        }

        public Task<OrderResult> ConfirmPaymentAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new OrderResult(false, "Not used."));
        }

        public Task<OrderResponse?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<OrderResponse?>(null);
        }

        public Task<IReadOnlyList<OrderResponse>> GetPatientOrdersAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<OrderResponse>>([]);
        }

        public Task<CustomerOrderListResult> GetMyOrdersAsync(
            Guid currentUserId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(MyOrdersResult);
        }

        public Task<CustomerOrderDetailResult> GetMyOrderByIdAsync(
            Guid currentUserId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(MyOrderDetailResult);
        }

        public Task<AdminOrderListResult> GetAdminOrdersAsync(
            AdminOrderQuery query,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AdminOrderListResult(true, "Not used.", []));
        }

        public Task<AdminOrderDetailResponse?> GetAdminOrderByIdAsync(
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AdminOrderDetailResponse?>(null);
        }

        public Task<AdminOrderResult> UpdateAdminOrderStatusAsync(
            Guid orderId,
            UpdateOrderStatusCommand command,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AdminOrderResult(false, "Not used."));
        }
    }

    private sealed class FakeEndpointAccessService : IEndpointAccessService
    {
        public Task<bool> PatientProfileBelongsToUserAsync(
            Guid userId,
            Guid patientProfileId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> AppointmentBelongsToUserAsync(
            Guid userId,
            Guid appointmentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> OrderBelongsToUserAsync(
            Guid userId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> CanManageDoctorProfileAsync(
            Guid userId,
            Guid doctorProfileId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}
