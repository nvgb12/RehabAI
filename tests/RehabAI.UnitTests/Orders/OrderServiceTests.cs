using RehabAI.Application.Orders;
using RehabAI.Domain.Enums;

namespace RehabAI.UnitTests.Orders;

public class OrderServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidCommand_CreatesPendingPaymentOrder()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);

        var result = await service.CreateAsync(repository.ValidCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(nameof(OrderStatus.PendingPayment), result.Order!.Status);
        Assert.Equal(nameof(PaymentStatus.Pending), result.Order.PaymentStatus);
        Assert.Equal(200000m, result.Order.TotalAmount);
        Assert.Equal("VND", result.Order.Currency);
        Assert.Equal("Stroke rehabilitation home address", result.Order.ShippingAddress);
        var item = Assert.Single(result.Order.Items);
        Assert.Equal(repository.ProductId, item.ProductId);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(100000m, item.UnitPrice);
        Assert.Equal(200000m, item.Subtotal);
        Assert.Equal(10, repository.GetProductStock(repository.ProductId));
    }

    [Fact]
    public async Task CreateAsync_WhenPatientIsNotFound_ReturnsPatientNotFound()
    {
        var repository = new FakeOrderRepository
        {
            PatientExists = false
        };
        var service = new OrderService(repository);

        var result = await service.CreateAsync(repository.ValidCommand());

        Assert.False(result.Succeeded);
        Assert.Equal(OrderFailureReason.PatientNotFound, result.FailureReason);
    }

    [Fact]
    public async Task CreateAsync_WhenProductIsNotFound_ReturnsProductNotFound()
    {
        var repository = new FakeOrderRepository();
        repository.RemoveProduct(repository.ProductId);
        var service = new OrderService(repository);

        var result = await service.CreateAsync(repository.ValidCommand());

        Assert.False(result.Succeeded);
        Assert.Equal(OrderFailureReason.ProductNotFound, result.FailureReason);
    }

    [Fact]
    public async Task CreateAsync_WhenProductIsInactive_ReturnsProductUnavailable()
    {
        var repository = new FakeOrderRepository();
        repository.SetProductActive(repository.ProductId, false);
        var service = new OrderService(repository);

        var result = await service.CreateAsync(repository.ValidCommand());

        Assert.False(result.Succeeded);
        Assert.Equal(OrderFailureReason.ProductUnavailable, result.FailureReason);
    }

    [Fact]
    public async Task CreateAsync_WhenProductIsDeleted_ReturnsProductUnavailable()
    {
        var repository = new FakeOrderRepository();
        repository.SetProductDeleted(repository.ProductId, true);
        var service = new OrderService(repository);

        var result = await service.CreateAsync(repository.ValidCommand());

        Assert.False(result.Succeeded);
        Assert.Equal(OrderFailureReason.ProductUnavailable, result.FailureReason);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateAsync_WhenQuantityIsNotPositive_ReturnsValidationFailure(int quantity)
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);

        var result = await service.CreateAsync(new CreateOrderCommand(
            repository.PatientProfileId,
            [new CreateOrderItemCommand(repository.ProductId, quantity)],
            "Stroke rehabilitation home address"));

        Assert.False(result.Succeeded);
        Assert.Equal(OrderFailureReason.Validation, result.FailureReason);
    }

    [Fact]
    public async Task CreateAsync_WhenQuantityExceedsStock_ReturnsQuantityExceedsStock()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);

        var result = await service.CreateAsync(new CreateOrderCommand(
            repository.PatientProfileId,
            [new CreateOrderItemCommand(repository.ProductId, 11)],
            "Stroke rehabilitation home address"));

        Assert.False(result.Succeeded);
        Assert.Equal(OrderFailureReason.QuantityExceedsStock, result.FailureReason);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderExists_ReturnsOrder()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);
        var create = await service.CreateAsync(repository.ValidCommand());

        var order = await service.GetByIdAsync(create.Order!.OrderId);

        Assert.NotNull(order);
        Assert.Equal(create.Order.OrderId, order!.OrderId);
    }

    [Fact]
    public async Task GetPatientOrdersAsync_WhenOrdersExist_ReturnsPatientOrders()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);
        var create = await service.CreateAsync(repository.ValidCommand());

        var orders = await service.GetPatientOrdersAsync(repository.PatientProfileId);

        var order = Assert.Single(orders);
        Assert.Equal(create.Order!.OrderId, order.OrderId);
    }

    [Fact]
    public async Task GetMyOrdersAsync_WhenUserIsNotActivePatient_ReturnsAccessDenied()
    {
        var repository = new FakeOrderRepository
        {
            CustomerHasPatientRole = false
        };
        var service = new OrderService(repository);

        var result = await service.GetMyOrdersAsync(repository.PatientUserId);

        Assert.False(result.Succeeded);
        Assert.Equal(OrderFailureReason.AccessDenied, result.FailureReason);
    }

    [Fact]
    public async Task GetMyOrdersAsync_WhenUserIsActivePatient_ReturnsOwnOrders()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);
        var create = await service.CreateAsync(repository.ValidCommand());

        var result = await service.GetMyOrdersAsync(repository.PatientUserId);

        Assert.True(result.Succeeded);
        var order = Assert.Single(result.Orders);
        Assert.Equal(create.Order!.OrderId, order.OrderId);
        Assert.Equal(create.Order.OrderNumber, order.OrderNumber);
        Assert.Equal(create.Order.TotalAmount, order.TotalAmount);
        Assert.Equal(create.Order.Currency, order.Currency);
        Assert.Equal(create.Order.Status, order.Status);
        Assert.Equal(create.Order.PaymentStatus, order.PaymentStatus);
    }

    [Fact]
    public async Task GetMyOrderByIdAsync_WhenOrderBelongsToPatient_ReturnsOrderDetail()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);
        var create = await service.CreateAsync(repository.ValidCommand());

        var result = await service.GetMyOrderByIdAsync(repository.PatientUserId, create.Order!.OrderId);

        Assert.True(result.Succeeded);
        Assert.Equal(create.Order.OrderId, result.Order!.OrderId);
        Assert.Equal(create.Order.ShippingAddress, result.Order.ShippingAddress);
        var item = Assert.Single(result.Order.Items);
        Assert.Equal(create.Order.Items[0].OrderItemId, item.OrderItemId);
        Assert.Equal(create.Order.Items[0].ProductId, item.ProductId);
    }

    [Fact]
    public async Task GetMyOrderByIdAsync_WhenOrderBelongsToAnotherPatient_ReturnsNotFound()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);
        var otherOrderId = repository.AddOtherPatientOrder();

        var result = await service.GetMyOrderByIdAsync(repository.PatientUserId, otherOrderId);

        Assert.False(result.Succeeded);
        Assert.Equal(OrderFailureReason.OrderNotFound, result.FailureReason);
    }

    [Fact]
    public async Task GetMyOrderByIdAsync_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);

        var result = await service.GetMyOrderByIdAsync(repository.PatientUserId, Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(OrderFailureReason.OrderNotFound, result.FailureReason);
    }

    [Fact]
    public async Task GetMyOrdersAsync_WhenOrderIsDeleted_ExcludesOrder()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);
        var create = await service.CreateAsync(repository.ValidCommand());
        repository.MarkOrderDeleted(create.Order!.OrderId);

        var result = await service.GetMyOrdersAsync(repository.PatientUserId);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Orders);
    }

    [Fact]
    public async Task GetMyOrderByIdAsync_WhenOrderItemIsDeleted_ExcludesOrderItem()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);
        var create = await service.CreateAsync(repository.ValidCommand());
        repository.MarkOrderItemDeleted(create.Order!.Items[0].OrderItemId);

        var result = await service.GetMyOrderByIdAsync(repository.PatientUserId, create.Order.OrderId);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Order!.Items);
    }

    [Fact]
    public async Task GetMyOrdersAsync_SortsOrdersByCreatedAtDescending()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);
        var older = await service.CreateAsync(repository.ValidCommand());
        var newer = await service.CreateAsync(repository.ValidCommand());
        repository.SetOrderCreatedAt(older.Order!.OrderId, DateTimeOffset.UtcNow.AddDays(-1));
        repository.SetOrderCreatedAt(newer.Order!.OrderId, DateTimeOffset.UtcNow);

        var result = await service.GetMyOrdersAsync(repository.PatientUserId);

        Assert.True(result.Succeeded);
        Assert.Collection(
            result.Orders,
            order => Assert.Equal(newer.Order.OrderId, order.OrderId),
            order => Assert.Equal(older.Order.OrderId, order.OrderId));
    }

    [Fact]
    public async Task ConfirmPaymentAsync_WhenOrderIsPending_ConfirmsPaymentAndReducesStock()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);
        var create = await service.CreateAsync(repository.ValidCommand());

        var result = await service.ConfirmPaymentAsync(create.Order!.OrderId);

        Assert.True(result.Succeeded);
        Assert.Equal(nameof(PaymentStatus.Paid), result.Order!.PaymentStatus);
        Assert.Equal(nameof(OrderStatus.Processing), result.Order.Status);
        Assert.Equal(8, repository.GetProductStock(repository.ProductId));
    }

    [Fact]
    public async Task ConfirmPaymentAsync_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);

        var result = await service.ConfirmPaymentAsync(Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(OrderFailureReason.OrderNotFound, result.FailureReason);
    }

    [Fact]
    public async Task ConfirmPaymentAsync_WhenOrderIsAlreadyPaid_ReturnsConflictReason()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);
        var create = await service.CreateAsync(repository.ValidCommand());
        await service.ConfirmPaymentAsync(create.Order!.OrderId);

        var result = await service.ConfirmPaymentAsync(create.Order.OrderId);

        Assert.False(result.Succeeded);
        Assert.Equal(OrderFailureReason.OrderPaymentNotPending, result.FailureReason);
    }

    [Fact]
    public async Task ConfirmPaymentAsync_WhenStockIsInsufficient_ReturnsConflictAndDoesNotReduceStock()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);
        var create = await service.CreateAsync(repository.ValidCommand());
        repository.SetProductStock(repository.ProductId, 1);

        var result = await service.ConfirmPaymentAsync(create.Order!.OrderId);

        Assert.False(result.Succeeded);
        Assert.Equal(OrderFailureReason.QuantityExceedsStock, result.FailureReason);
        Assert.Equal(1, repository.GetProductStock(repository.ProductId));
        var order = await service.GetByIdAsync(create.Order.OrderId);
        Assert.Equal(nameof(PaymentStatus.Pending), order!.PaymentStatus);
        Assert.Equal(nameof(OrderStatus.PendingPayment), order.Status);
    }

    [Fact]
    public async Task GetAdminOrdersAsync_WhenOrdersExist_ReturnsNonDeletedOrders()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);
        var first = await service.CreateAsync(repository.ValidCommand());
        await service.CreateAsync(repository.ValidCommand());
        repository.MarkOrderDeleted(first.Order!.OrderId);

        var result = await service.GetAdminOrdersAsync(new AdminOrderQuery(null, null, null, null));

        Assert.True(result.Succeeded);
        var order = Assert.Single(result.Orders);
        Assert.NotEqual(first.Order.OrderId, order.OrderId);
        Assert.Equal(repository.PatientProfileId, order.PatientProfileId);
        Assert.Equal("Stroke Rehab Patient", order.PatientName);
        Assert.Equal("patient@test.com", order.PatientEmail);
    }

    [Fact]
    public async Task GetAdminOrderByIdAsync_WhenOrderExists_ReturnsOrderDetail()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);
        var create = await service.CreateAsync(repository.ValidCommand());

        var order = await service.GetAdminOrderByIdAsync(create.Order!.OrderId);

        Assert.NotNull(order);
        Assert.Equal(create.Order.OrderId, order!.OrderId);
        Assert.Equal(repository.PatientProfileId, order.PatientProfileId);
        Assert.Single(order.Items);
    }

    [Fact]
    public async Task UpdateAdminOrderStatusAsync_WhenProcessingToCompleted_Succeeds()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);
        var create = await service.CreateAsync(repository.ValidCommand());
        await service.ConfirmPaymentAsync(create.Order!.OrderId);

        var result = await service.UpdateAdminOrderStatusAsync(
            create.Order.OrderId,
            new UpdateOrderStatusCommand(nameof(OrderStatus.Completed)));

        Assert.True(result.Succeeded);
        Assert.Equal(nameof(OrderStatus.Completed), result.Order!.Status);
        Assert.Equal(nameof(PaymentStatus.Paid), result.Order.PaymentStatus);
    }

    [Fact]
    public async Task UpdateAdminOrderStatusAsync_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);

        var result = await service.UpdateAdminOrderStatusAsync(
            Guid.NewGuid(),
            new UpdateOrderStatusCommand(nameof(OrderStatus.Completed)));

        Assert.False(result.Succeeded);
        Assert.Equal(OrderFailureReason.OrderNotFound, result.FailureReason);
    }

    [Fact]
    public async Task UpdateAdminOrderStatusAsync_WhenTransitionIsInvalid_ReturnsInvalidTransition()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);
        var create = await service.CreateAsync(repository.ValidCommand());

        var result = await service.UpdateAdminOrderStatusAsync(
            create.Order!.OrderId,
            new UpdateOrderStatusCommand(nameof(OrderStatus.Completed)));

        Assert.False(result.Succeeded);
        Assert.Equal(OrderFailureReason.InvalidStatusTransition, result.FailureReason);
        Assert.Equal("Invalid order status transition.", result.Message);
    }

    [Theory]
    [InlineData("Confirmed")]
    [InlineData("Delivered")]
    public async Task UpdateAdminOrderStatusAsync_WhenStatusValueIsInvalid_ReturnsAllowedValues(string status)
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);
        var create = await service.CreateAsync(repository.ValidCommand());

        var result = await service.UpdateAdminOrderStatusAsync(
            create.Order!.OrderId,
            new UpdateOrderStatusCommand(status));

        Assert.False(result.Succeeded);
        Assert.Equal(OrderFailureReason.InvalidStatus, result.FailureReason);
        Assert.Equal(
            $"Order status is invalid. Allowed values are: {OrderStatusCatalog.AdminUpdateStatusValuesText}.",
            result.Message);
    }

    [Fact]
    public async Task UpdateAdminOrderStatusAsync_WhenMovingBackward_ReturnsInvalidTransition()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);
        var create = await service.CreateAsync(repository.ValidCommand());
        await service.ConfirmPaymentAsync(create.Order!.OrderId);
        await service.UpdateAdminOrderStatusAsync(
            create.Order.OrderId,
            new UpdateOrderStatusCommand(nameof(OrderStatus.Shipped)));

        var result = await service.UpdateAdminOrderStatusAsync(
            create.Order.OrderId,
            new UpdateOrderStatusCommand(nameof(OrderStatus.Processing)));

        Assert.False(result.Succeeded);
        Assert.Equal(OrderFailureReason.InvalidStatusTransition, result.FailureReason);
        Assert.Equal("Invalid order status transition.", result.Message);
    }

    [Fact]
    public async Task UpdateAdminOrderStatusAsync_WhenOrderIsFinalStatus_ReturnsInvalidTransition()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository);
        var create = await service.CreateAsync(repository.ValidCommand());
        await service.ConfirmPaymentAsync(create.Order!.OrderId);
        await service.UpdateAdminOrderStatusAsync(
            create.Order.OrderId,
            new UpdateOrderStatusCommand(nameof(OrderStatus.Completed)));

        var result = await service.UpdateAdminOrderStatusAsync(
            create.Order.OrderId,
            new UpdateOrderStatusCommand(nameof(OrderStatus.Completed)));

        Assert.False(result.Succeeded);
        Assert.Equal(OrderFailureReason.InvalidStatusTransition, result.FailureReason);
        Assert.Equal("Invalid order status transition.", result.Message);
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        private readonly Dictionary<Guid, ProductOrderState> products = [];
        private readonly List<OrderRecord> orders = [];
        private readonly HashSet<Guid> deletedOrders = [];
        private readonly HashSet<Guid> deletedOrderItems = [];
        private readonly Dictionary<Guid, DateTimeOffset> createdAtByOrderId = [];
        private readonly Dictionary<Guid, DateTimeOffset?> updatedAtByOrderId = [];

        public FakeOrderRepository()
        {
            products[ProductId] = new ProductOrderState(
                ProductId,
                "Stroke Recovery Therapy Ball",
                100000m,
                "VND",
                10,
                true,
                false);
        }

        public Guid PatientProfileId { get; } = Guid.NewGuid();
        public Guid PatientUserId { get; } = Guid.NewGuid();
        public Guid OtherPatientProfileId { get; } = Guid.NewGuid();
        public Guid OtherPatientUserId { get; } = Guid.NewGuid();
        public Guid ProductId { get; } = Guid.NewGuid();
        public bool PatientExists { get; set; } = true;
        public bool CustomerAccessExists { get; set; } = true;
        public bool CustomerHasPatientRole { get; set; } = true;
        public int CustomerStatus { get; set; } = (int)AccountStatus.Active;

        public CreateOrderCommand ValidCommand()
        {
            return new CreateOrderCommand(
                PatientProfileId,
                [new CreateOrderItemCommand(ProductId, 2)],
                "Stroke rehabilitation home address");
        }

        public int GetProductStock(Guid productId)
        {
            return products[productId].StockQuantity;
        }

        public void RemoveProduct(Guid productId)
        {
            products.Remove(productId);
        }

        public void SetProductActive(Guid productId, bool isActive)
        {
            products[productId] = products[productId] with { IsActive = isActive };
        }

        public void SetProductDeleted(Guid productId, bool isDeleted)
        {
            products[productId] = products[productId] with { IsDeleted = isDeleted };
        }

        public void SetProductStock(Guid productId, int stockQuantity)
        {
            products[productId] = products[productId] with { StockQuantity = stockQuantity };
        }

        public void MarkOrderDeleted(Guid orderId)
        {
            deletedOrders.Add(orderId);
        }

        public void MarkOrderItemDeleted(Guid orderItemId)
        {
            deletedOrderItems.Add(orderItemId);
        }

        public void SetOrderCreatedAt(Guid orderId, DateTimeOffset createdAt)
        {
            createdAtByOrderId[orderId] = createdAt;
        }

        public Guid AddOtherPatientOrder()
        {
            var order = new OrderRecord(
                Guid.NewGuid(),
                OtherPatientProfileId,
                OtherPatientUserId,
                "ORD-OTHER",
                OrderStatus.Processing,
                PaymentStatus.Paid,
                100000m,
                "VND",
                "Other stroke rehabilitation home address",
                [
                    new OrderItemRecord(
                        Guid.NewGuid(),
                        ProductId,
                        "Stroke Recovery Therapy Ball",
                        1,
                        100000m,
                        100000m)
                ]);

            orders.Add(order);
            createdAtByOrderId[order.OrderId] = DateTimeOffset.UtcNow;
            updatedAtByOrderId[order.OrderId] = null;

            return order.OrderId;
        }

        public Task<OrderPatientState?> GetPatientStateAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken = default)
        {
            if (!PatientExists || patientProfileId != PatientProfileId)
            {
                return Task.FromResult<OrderPatientState?>(null);
            }

            return Task.FromResult<OrderPatientState?>(new OrderPatientState(PatientProfileId, PatientUserId));
        }

        public Task<IReadOnlyList<ProductOrderState>> GetProductStatesAsync(
            IReadOnlyCollection<Guid> productIds,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ProductOrderState>>(
                products.Values.Where(product => productIds.Contains(product.ProductId)).ToList());
        }

        public Task<OrderRecord> CreateAsync(OrderDraft draft, CancellationToken cancellationToken = default)
        {
            var order = new OrderRecord(
                Guid.NewGuid(),
                draft.PatientProfileId,
                draft.PatientUserId,
                draft.OrderNumber,
                OrderStatus.PendingPayment,
                PaymentStatus.Pending,
                draft.TotalAmount,
                draft.Currency,
                draft.ShippingAddress,
                draft.Items.Select(item => new OrderItemRecord(
                    Guid.NewGuid(),
                    item.ProductId,
                    item.ProductName,
                    item.Quantity,
                    item.UnitPrice,
                    item.Subtotal)).ToList());

            orders.Add(order);
            createdAtByOrderId[order.OrderId] = DateTimeOffset.UtcNow;
            updatedAtByOrderId[order.OrderId] = null;

            return Task.FromResult(order);
        }

        public Task<OrderRepositoryResult> ConfirmPaymentPlaceholderAsync(
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            var index = orders.FindIndex(order =>
                order.OrderId == orderId &&
                !deletedOrders.Contains(order.OrderId));

            if (index < 0)
            {
                return Task.FromResult(new OrderRepositoryResult(
                    false,
                    null,
                    OrderFailureReason.OrderNotFound,
                    "Order was not found."));
            }

            var order = orders[index];

            if (order.PaymentStatus != PaymentStatus.Pending)
            {
                return Task.FromResult(new OrderRepositoryResult(
                    false,
                    null,
                    OrderFailureReason.OrderPaymentNotPending,
                    "Only orders with Pending payment status can be confirmed."));
            }

            foreach (var item in order.Items)
            {
                if (!products.TryGetValue(item.ProductId, out var product))
                {
                    return Task.FromResult(new OrderRepositoryResult(
                        false,
                        null,
                        OrderFailureReason.ProductNotFound,
                        "Product was not found."));
                }

                if (!product.IsActive || product.IsDeleted)
                {
                    return Task.FromResult(new OrderRepositoryResult(
                        false,
                        null,
                        OrderFailureReason.ProductUnavailable,
                        "Product is no longer available."));
                }

                if (product.StockQuantity < item.Quantity)
                {
                    return Task.FromResult(new OrderRepositoryResult(
                        false,
                        null,
                        OrderFailureReason.QuantityExceedsStock,
                        "Current product stock is insufficient for this order."));
                }
            }

            foreach (var item in order.Items)
            {
                var product = products[item.ProductId];
                products[item.ProductId] = product with { StockQuantity = product.StockQuantity - item.Quantity };
            }

            var paidOrder = order with
            {
                Status = OrderStatus.Processing,
                PaymentStatus = PaymentStatus.Paid
            };

            orders[index] = paidOrder;
            updatedAtByOrderId[orderId] = DateTimeOffset.UtcNow;

            return Task.FromResult(new OrderRepositoryResult(true, paidOrder, null, null));
        }

        public Task<OrderRecord?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(orders.SingleOrDefault(order =>
                order.OrderId == orderId &&
                !deletedOrders.Contains(order.OrderId)));
        }

        public Task<IReadOnlyList<OrderRecord>> GetByPatientProfileIdAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<OrderRecord>>(
                orders
                    .Where(order =>
                        order.PatientProfileId == patientProfileId &&
                        !deletedOrders.Contains(order.OrderId))
                    .ToList());
        }

        public Task<CustomerOrderAccessState?> GetCustomerOrderAccessStateAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            if (!CustomerAccessExists || userId != PatientUserId)
            {
                return Task.FromResult<CustomerOrderAccessState?>(null);
            }

            return Task.FromResult<CustomerOrderAccessState?>(new CustomerOrderAccessState(
                PatientUserId,
                PatientProfileId,
                CustomerStatus,
                CustomerHasPatientRole));
        }

        public Task<IReadOnlyList<CustomerOrderSummaryRecord>> GetCustomerOrdersByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<CustomerOrderSummaryRecord>>(
                orders
                    .Where(order =>
                        order.PatientUserId == userId &&
                        !deletedOrders.Contains(order.OrderId))
                    .OrderByDescending(order => GetCreatedAt(order.OrderId))
                    .Select(ToCustomerSummaryRecord)
                    .ToList());
        }

        public Task<CustomerOrderDetailRecord?> GetCustomerOrderByIdAsync(
            Guid userId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            var order = orders.SingleOrDefault(order =>
                order.OrderId == orderId &&
                order.PatientUserId == userId &&
                !deletedOrders.Contains(order.OrderId));

            return Task.FromResult(order is null ? null : ToCustomerDetailRecord(order));
        }

        public Task<IReadOnlyList<AdminOrderRecord>> GetAdminOrdersAsync(
            AdminOrderFilter filter,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<OrderRecord> query = orders.Where(order => !deletedOrders.Contains(order.OrderId));

            if (filter.Status.HasValue)
            {
                query = query.Where(order => order.Status == filter.Status.Value);
            }

            if (filter.PaymentStatus.HasValue)
            {
                query = query.Where(order => order.PaymentStatus == filter.PaymentStatus.Value);
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(order => GetCreatedAt(order.OrderId) >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(order => GetCreatedAt(order.OrderId) <= filter.ToDate.Value);
            }

            return Task.FromResult<IReadOnlyList<AdminOrderRecord>>(
                query
                    .OrderByDescending(order => GetCreatedAt(order.OrderId))
                    .Select(ToAdminRecord)
                    .ToList());
        }

        public Task<AdminOrderRecord?> GetAdminOrderByIdAsync(
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            var order = orders.SingleOrDefault(order =>
                order.OrderId == orderId &&
                !deletedOrders.Contains(order.OrderId));

            return Task.FromResult(order is null ? null : ToAdminRecord(order));
        }

        public Task<AdminOrderRecord?> UpdateAdminOrderStatusAsync(
            Guid orderId,
            OrderStatus status,
            CancellationToken cancellationToken = default)
        {
            var index = orders.FindIndex(order =>
                order.OrderId == orderId &&
                !deletedOrders.Contains(order.OrderId));

            if (index < 0)
            {
                return Task.FromResult<AdminOrderRecord?>(null);
            }

            var updated = orders[index] with { Status = status };
            orders[index] = updated;
            updatedAtByOrderId[orderId] = DateTimeOffset.UtcNow;

            return Task.FromResult<AdminOrderRecord?>(ToAdminRecord(updated));
        }

        private DateTimeOffset GetCreatedAt(Guid orderId)
        {
            return createdAtByOrderId.TryGetValue(orderId, out var createdAt)
                ? createdAt
                : DateTimeOffset.UtcNow;
        }

        private AdminOrderRecord ToAdminRecord(OrderRecord order)
        {
            return new AdminOrderRecord(
                order.OrderId,
                order.OrderNumber,
                order.PatientProfileId,
                "Stroke Rehab Patient",
                "patient@test.com",
                order.Status,
                order.PaymentStatus,
                order.TotalAmount,
                order.Currency,
                order.ShippingAddress,
                GetCreatedAt(order.OrderId),
                updatedAtByOrderId.GetValueOrDefault(order.OrderId),
                order.Items);
        }

        private CustomerOrderSummaryRecord ToCustomerSummaryRecord(OrderRecord order)
        {
            return new CustomerOrderSummaryRecord(
                order.OrderId,
                order.OrderNumber,
                GetCreatedAt(order.OrderId),
                order.Status,
                order.PaymentStatus,
                order.TotalAmount,
                order.Currency);
        }

        private CustomerOrderDetailRecord ToCustomerDetailRecord(OrderRecord order)
        {
            return new CustomerOrderDetailRecord(
                order.OrderId,
                order.OrderNumber,
                GetCreatedAt(order.OrderId),
                updatedAtByOrderId.GetValueOrDefault(order.OrderId),
                order.ShippingAddress,
                order.Status,
                order.PaymentStatus,
                order.TotalAmount,
                order.Currency,
                order.Items
                    .Where(item => !deletedOrderItems.Contains(item.OrderItemId))
                    .ToList());
        }
    }
}
