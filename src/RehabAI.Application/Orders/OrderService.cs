using RehabAI.Domain.Enums;

namespace RehabAI.Application.Orders;

public sealed class OrderService(IOrderRepository repository) : IOrderService
{
    private const string DefaultCurrency = "VND";

    public async Task<OrderResult> CreateAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = ValidateCommand(command);

        if (!validationResult.Succeeded)
        {
            return validationResult;
        }

        var patient = await repository.GetPatientStateAsync(command.PatientProfileId, cancellationToken);

        if (patient is null)
        {
            return Failed("Patient profile was not found.", OrderFailureReason.PatientNotFound);
        }

        var groupedItems = command.Items
            .GroupBy(item => item.ProductId)
            .Select(group => new CreateOrderItemCommand(group.Key, group.Sum(item => item.Quantity)))
            .ToList();

        var productStates = await repository.GetProductStatesAsync(
            groupedItems.Select(item => item.ProductId).ToList(),
            cancellationToken);
        var productById = productStates.ToDictionary(product => product.ProductId);
        var orderItems = new List<OrderItemDraft>();
        string? orderCurrency = null;

        foreach (var item in groupedItems)
        {
            if (!productById.TryGetValue(item.ProductId, out var product))
            {
                return Failed("Product was not found.", OrderFailureReason.ProductNotFound);
            }

            if (!product.IsActive || product.IsDeleted)
            {
                return Failed("Product was not found.", OrderFailureReason.ProductUnavailable);
            }

            if (item.Quantity > product.StockQuantity)
            {
                return Failed(
                    "Order quantity exceeds available product stock.",
                    OrderFailureReason.QuantityExceedsStock);
            }

            var currency = NormalizeCurrency(product.Currency);
            orderCurrency ??= currency;
            var subtotal = product.Price * item.Quantity;

            orderItems.Add(new OrderItemDraft(
                product.ProductId,
                product.Name,
                item.Quantity,
                product.Price,
                subtotal));
        }

        var draft = new OrderDraft(
            patient.PatientProfileId,
            patient.UserId,
            CreateOrderNumber(),
            orderCurrency ?? DefaultCurrency,
            orderItems.Sum(item => item.Subtotal),
            NormalizeOptional(command.ShippingAddress),
            orderItems);

        var created = await repository.CreateAsync(draft, cancellationToken);

        return new OrderResult(
            true,
            "Order created successfully.",
            ToResponse(created));
    }

    public async Task<OrderResponse?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty)
        {
            return null;
        }

        var order = await repository.GetByIdAsync(orderId, cancellationToken);

        return order is null ? null : ToResponse(order);
    }

    public async Task<OrderResult> ConfirmPaymentAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty)
        {
            return Failed("Order id is required.", OrderFailureReason.Validation);
        }

        var result = await repository.ConfirmPaymentPlaceholderAsync(orderId, cancellationToken);

        if (!result.Succeeded)
        {
            return Failed(
                result.Message ?? "Order payment could not be confirmed.",
                result.FailureReason ?? OrderFailureReason.Validation);
        }

        return new OrderResult(
            true,
            "Order payment confirmed successfully.",
            ToResponse(result.Order!));
    }

    public async Task<IReadOnlyList<OrderResponse>> GetPatientOrdersAsync(
        Guid patientProfileId,
        CancellationToken cancellationToken = default)
    {
        if (patientProfileId == Guid.Empty)
        {
            return [];
        }

        var orders = await repository.GetByPatientProfileIdAsync(patientProfileId, cancellationToken);

        return orders.Select(ToResponse).ToList();
    }

    public async Task<CustomerOrderListResult> GetMyOrdersAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var accessResult = await ValidateCustomerAccessAsync(currentUserId, cancellationToken);

        if (!accessResult.Succeeded)
        {
            return new CustomerOrderListResult(
                false,
                accessResult.Message,
                [],
                accessResult.FailureReason);
        }

        var orders = await repository.GetCustomerOrdersByUserIdAsync(currentUserId, cancellationToken);

        return new CustomerOrderListResult(
            true,
            "Orders retrieved successfully.",
            orders.Select(ToCustomerSummaryResponse).ToList());
    }

    public async Task<CustomerOrderDetailResult> GetMyOrderByIdAsync(
        Guid currentUserId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty)
        {
            return CustomerFailed("Order was not found.", OrderFailureReason.OrderNotFound);
        }

        var accessResult = await ValidateCustomerAccessAsync(currentUserId, cancellationToken);

        if (!accessResult.Succeeded)
        {
            return CustomerFailed(accessResult.Message, accessResult.FailureReason);
        }

        var order = await repository.GetCustomerOrderByIdAsync(currentUserId, orderId, cancellationToken);

        return order is null
            ? CustomerFailed("Order was not found.", OrderFailureReason.OrderNotFound)
            : new CustomerOrderDetailResult(
                true,
                "Order retrieved successfully.",
                ToCustomerDetailResponse(order));
    }

    public async Task<AdminOrderListResult> GetAdminOrdersAsync(
        AdminOrderQuery query,
        CancellationToken cancellationToken = default)
    {
        var filterResult = BuildAdminFilter(query);

        if (!filterResult.Succeeded)
        {
            return new AdminOrderListResult(
                false,
                filterResult.Message,
                [],
                filterResult.FailureReason);
        }

        var orders = await repository.GetAdminOrdersAsync(filterResult.Filter!, cancellationToken);

        return new AdminOrderListResult(
            true,
            "Orders retrieved successfully.",
            orders.Select(ToAdminSummaryResponse).ToList());
    }

    public async Task<AdminOrderDetailResponse?> GetAdminOrderByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty)
        {
            return null;
        }

        var order = await repository.GetAdminOrderByIdAsync(orderId, cancellationToken);

        return order is null ? null : ToAdminDetailResponse(order);
    }

    public async Task<AdminOrderResult> UpdateAdminOrderStatusAsync(
        Guid orderId,
        UpdateOrderStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty)
        {
            return AdminFailed("Order id is required.", OrderFailureReason.Validation);
        }

        if (!TryParseRequiredOrderStatus(command.Status, out var targetStatus))
        {
            return AdminFailed(
                $"Order status is invalid. Allowed values are: {OrderStatusCatalog.AdminUpdateStatusValuesText}.",
                OrderFailureReason.InvalidStatus);
        }

        var existing = await repository.GetAdminOrderByIdAsync(orderId, cancellationToken);

        if (existing is null)
        {
            return AdminFailed("Order was not found.", OrderFailureReason.OrderNotFound);
        }

        if (!CanUpdateOrderStatus(existing.Status, existing.PaymentStatus, targetStatus!.Value))
        {
            return AdminFailed(
                "Invalid order status transition.",
                OrderFailureReason.InvalidStatusTransition);
        }

        var updated = await repository.UpdateAdminOrderStatusAsync(orderId, targetStatus.Value, cancellationToken);

        return updated is null
            ? AdminFailed("Order was not found.", OrderFailureReason.OrderNotFound)
            : new AdminOrderResult(
                true,
                "Order status updated successfully.",
                ToAdminDetailResponse(updated));
    }

    private static OrderResult ValidateCommand(CreateOrderCommand command)
    {
        if (command.PatientProfileId == Guid.Empty)
        {
            return Failed("Patient profile id is required.", OrderFailureReason.Validation);
        }

        if (command.Items.Count == 0)
        {
            return Failed("Order must contain at least one item.", OrderFailureReason.Validation);
        }

        foreach (var item in command.Items)
        {
            if (item.ProductId == Guid.Empty)
            {
                return Failed("Product id is required.", OrderFailureReason.Validation);
            }

            if (item.Quantity <= 0)
            {
                return Failed("Quantity must be greater than 0.", OrderFailureReason.Validation);
            }
        }

        return new OrderResult(true, string.Empty);
    }

    private static OrderResponse ToResponse(OrderRecord record)
    {
        return new OrderResponse(
            record.OrderId,
            record.PatientProfileId,
            record.PatientUserId,
            record.OrderNumber,
            record.Status.ToString(),
            record.PaymentStatus.ToString(),
            record.TotalAmount,
            record.Currency,
            record.ShippingAddress,
            record.Items.Select(ToItemResponse).ToList());
    }

    private static OrderItemResponse ToItemResponse(OrderItemRecord record)
    {
        return new OrderItemResponse(
            record.OrderItemId,
            record.ProductId,
            record.ProductName,
            record.Quantity,
            record.UnitPrice,
            record.Subtotal);
    }

    private static AdminOrderSummaryResponse ToAdminSummaryResponse(AdminOrderRecord record)
    {
        return new AdminOrderSummaryResponse(
            record.OrderId,
            record.OrderNumber,
            record.PatientProfileId,
            record.PatientName,
            record.PatientEmail,
            record.Status.ToString(),
            record.PaymentStatus.ToString(),
            record.TotalAmount,
            record.Currency,
            record.ShippingAddress,
            record.CreatedAt,
            record.UpdatedAt);
    }

    private static AdminOrderDetailResponse ToAdminDetailResponse(AdminOrderRecord record)
    {
        return new AdminOrderDetailResponse(
            record.OrderId,
            record.OrderNumber,
            record.PatientProfileId,
            record.PatientName,
            record.PatientEmail,
            record.Status.ToString(),
            record.PaymentStatus.ToString(),
            record.TotalAmount,
            record.Currency,
            record.ShippingAddress,
            record.CreatedAt,
            record.UpdatedAt,
            record.Items.Select(ToItemResponse).ToList());
    }

    private static CustomerOrderSummaryResponse ToCustomerSummaryResponse(CustomerOrderSummaryRecord record)
    {
        return new CustomerOrderSummaryResponse(
            record.OrderId,
            record.OrderNumber,
            record.CreatedAt,
            record.TotalAmount,
            record.Currency,
            record.Status.ToString(),
            record.PaymentStatus.ToString());
    }

    private static CustomerOrderDetailResponse ToCustomerDetailResponse(CustomerOrderDetailRecord record)
    {
        return new CustomerOrderDetailResponse(
            record.OrderId,
            record.OrderNumber,
            record.CreatedAt,
            record.UpdatedAt,
            record.ShippingAddress,
            record.TotalAmount,
            record.Currency,
            record.Status.ToString(),
            record.PaymentStatus.ToString(),
            record.Items.Select(ToCustomerItemResponse).ToList());
    }

    private static CustomerOrderItemResponse ToCustomerItemResponse(OrderItemRecord record)
    {
        return new CustomerOrderItemResponse(
            record.OrderItemId,
            record.ProductId,
            record.ProductName,
            record.Quantity,
            record.UnitPrice,
            record.Subtotal);
    }

    private static AdminFilterResult BuildAdminFilter(AdminOrderQuery query)
    {
        if (!TryParseOptionalOrderStatus(query.Status, out var status))
        {
            return AdminFilterResult.Failed(
                $"Order status filter is invalid. Allowed values are: {OrderStatusCatalog.AllStatusValuesText}.",
                OrderFailureReason.InvalidStatus);
        }

        if (!TryParsePaymentStatus(query.PaymentStatus, out var paymentStatus))
        {
            return AdminFilterResult.Failed("Payment status filter is invalid.", OrderFailureReason.InvalidStatus);
        }

        if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate > query.ToDate)
        {
            return AdminFilterResult.Failed(
                "fromDate must be before or equal to toDate.",
                OrderFailureReason.Validation);
        }

        return AdminFilterResult.Success(new AdminOrderFilter(
            status,
            paymentStatus,
            query.FromDate,
            query.ToDate));
    }

    private static bool TryParseOptionalOrderStatus(string? value, out OrderStatus? status)
    {
        return TryParseOrderStatus(value, OrderStatusCatalog.AllStatuses, false, out status);
    }

    private static bool TryParseRequiredOrderStatus(string? value, out OrderStatus? status)
    {
        return TryParseOrderStatus(value, OrderStatusCatalog.AdminUpdateStatuses, true, out status);
    }

    private static bool TryParseOrderStatus(
        string? value,
        IReadOnlyCollection<OrderStatus> allowedStatuses,
        bool required,
        out OrderStatus? status)
    {
        status = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return !required;
        }

        if (!Enum.TryParse<OrderStatus>(value.Trim(), true, out var parsed) ||
            !allowedStatuses.Contains(parsed))
        {
            return false;
        }

        status = parsed;

        return true;
    }

    private static bool TryParsePaymentStatus(string? value, out PaymentStatus? status)
    {
        status = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!Enum.TryParse<PaymentStatus>(value.Trim(), true, out var parsed) ||
            !Enum.IsDefined(parsed))
        {
            return false;
        }

        status = parsed;

        return true;
    }

    private static bool CanUpdateOrderStatus(
        OrderStatus currentStatus,
        PaymentStatus paymentStatus,
        OrderStatus targetStatus)
    {
        if (currentStatus is OrderStatus.Completed or OrderStatus.Cancelled or OrderStatus.Refunded)
        {
            return false;
        }

        if (currentStatus == targetStatus)
        {
            return true;
        }

        if (paymentStatus != PaymentStatus.Paid)
        {
            return currentStatus == OrderStatus.PendingPayment &&
                targetStatus == OrderStatus.Cancelled;
        }

        return currentStatus switch
        {
            OrderStatus.PendingPayment => targetStatus is
                OrderStatus.Paid or
                OrderStatus.Processing or
                OrderStatus.Cancelled,
            OrderStatus.Paid => targetStatus is
                OrderStatus.Processing or
                OrderStatus.Shipped or
                OrderStatus.Completed or
                OrderStatus.Cancelled,
            OrderStatus.Processing => targetStatus is
                OrderStatus.Shipped or
                OrderStatus.Completed or
                OrderStatus.Cancelled,
            OrderStatus.Shipped => targetStatus is
                OrderStatus.Completed or
                OrderStatus.Cancelled,
            _ => false
        };
    }

    private async Task<CustomerAccessResult> ValidateCustomerAccessAsync(
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        if (currentUserId == Guid.Empty)
        {
            return CustomerAccessResult.Failed(
                "Authenticated user is required.",
                OrderFailureReason.AccessDenied);
        }

        var customer = await repository.GetCustomerOrderAccessStateAsync(currentUserId, cancellationToken);

        if (customer is null ||
            customer.Status != (int)AccountStatus.Active ||
            !customer.HasPatientRole)
        {
            return CustomerAccessResult.Failed(
                "Only active Patient users can view customer orders.",
                OrderFailureReason.AccessDenied);
        }

        return CustomerAccessResult.Success();
    }

    private static string CreateOrderNumber()
    {
        return $"ORD-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }

    private static string NormalizeCurrency(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DefaultCurrency : value.Trim().ToUpperInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static OrderResult Failed(string message, OrderFailureReason reason)
    {
        return new OrderResult(false, message, FailureReason: reason);
    }

    private static AdminOrderResult AdminFailed(string message, OrderFailureReason reason)
    {
        return new AdminOrderResult(false, message, FailureReason: reason);
    }

    private static CustomerOrderDetailResult CustomerFailed(string message, OrderFailureReason reason)
    {
        return new CustomerOrderDetailResult(false, message, FailureReason: reason);
    }

    private sealed record AdminFilterResult(
        bool Succeeded,
        string Message,
        AdminOrderFilter? Filter,
        OrderFailureReason? FailureReason = null)
    {
        public static AdminFilterResult Success(AdminOrderFilter filter)
        {
            return new AdminFilterResult(true, string.Empty, filter);
        }

        public static AdminFilterResult Failed(string message, OrderFailureReason reason)
        {
            return new AdminFilterResult(false, message, null, reason);
        }
    }

    private sealed record CustomerAccessResult(
        bool Succeeded,
        string Message,
        OrderFailureReason FailureReason)
    {
        public static CustomerAccessResult Success()
        {
            return new CustomerAccessResult(true, string.Empty, OrderFailureReason.Validation);
        }

        public static CustomerAccessResult Failed(string message, OrderFailureReason reason)
        {
            return new CustomerAccessResult(false, message, reason);
        }
    }
}
