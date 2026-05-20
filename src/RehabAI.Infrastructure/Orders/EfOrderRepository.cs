using System.Data;
using Microsoft.EntityFrameworkCore;
using RehabAI.Application.Orders;
using RehabAI.Domain.Entities;
using RehabAI.Domain.Enums;
using RehabAI.Infrastructure.Database;

namespace RehabAI.Infrastructure.Orders;

public sealed class EfOrderRepository(AppDbContext dbContext) : IOrderRepository
{
    public async Task<OrderPatientState?> GetPatientStateAsync(
        Guid patientProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PatientProfiles
            .Where(profile =>
                profile.Id == patientProfileId &&
                !profile.IsDeleted &&
                profile.User != null &&
                !profile.User.IsDeleted)
            .Select(profile => new OrderPatientState(profile.Id, profile.UserId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductOrderState>> GetProductStatesAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Products
            .Where(product => productIds.Contains(product.Id))
            .Select(product => new ProductOrderState(
                product.Id,
                product.Name,
                product.Price,
                product.Currency,
                product.StockQuantity,
                product.IsActive,
                product.IsDeleted))
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderRecord> CreateAsync(OrderDraft draft, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var order = new Order
        {
            UserId = draft.PatientUserId,
            OrderNumber = draft.OrderNumber,
            Status = OrderStatus.PendingPayment,
            PaymentStatus = PaymentStatus.Pending,
            TotalAmount = draft.TotalAmount,
            Currency = draft.Currency,
            ShippingAddress = draft.ShippingAddress,
            CreatedAt = now
        };

        foreach (var item in draft.Items)
        {
            order.Items.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Subtotal = item.Subtotal,
                CreatedAt = now
            });
        }

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToRecord(order, draft.PatientProfileId);
    }

    public async Task<OrderRepositoryResult> ConfirmPaymentPlaceholderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var order = await dbContext.Orders
            .Include(order => order.Items)
            .SingleOrDefaultAsync(order => order.Id == orderId && !order.IsDeleted, cancellationToken);

        if (order is null)
        {
            return Failed("Order was not found.", OrderFailureReason.OrderNotFound);
        }

        if (order.PaymentStatus != PaymentStatus.Pending)
        {
            return Failed(
                "Only orders with Pending payment status can be confirmed.",
                OrderFailureReason.OrderPaymentNotPending);
        }

        var productIds = order.Items.Select(item => item.ProductId).Distinct().ToList();
        var products = await dbContext.Products
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        foreach (var item in order.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
            {
                return Failed("Product was not found.", OrderFailureReason.ProductNotFound);
            }

            if (!product.IsActive || product.IsDeleted)
            {
                return Failed("Product is no longer available.", OrderFailureReason.ProductUnavailable);
            }

            if (product.StockQuantity < item.Quantity)
            {
                return Failed(
                    "Current product stock is insufficient for this order.",
                    OrderFailureReason.QuantityExceedsStock);
            }
        }

        var now = DateTimeOffset.UtcNow;

        foreach (var item in order.Items)
        {
            products[item.ProductId].StockQuantity -= item.Quantity;
            products[item.ProductId].UpdatedAt = now;
        }

        order.PaymentStatus = PaymentStatus.Paid;
        order.Status = OrderStatus.Processing;
        order.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var patientProfileId = await GetPatientProfileIdAsync(order.UserId, cancellationToken);

        return new OrderRepositoryResult(
            true,
            ToRecord(order, patientProfileId),
            null,
            null);
    }

    public async Task<OrderRecord?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders
            .Include(order => order.Items)
            .SingleOrDefaultAsync(order => order.Id == orderId && !order.IsDeleted, cancellationToken);

        if (order is null)
        {
            return null;
        }

        var patientProfileId = await GetPatientProfileIdAsync(order.UserId, cancellationToken);

        return patientProfileId == Guid.Empty ? null : ToRecord(order, patientProfileId);
    }

    public async Task<IReadOnlyList<OrderRecord>> GetByPatientProfileIdAsync(
        Guid patientProfileId,
        CancellationToken cancellationToken = default)
    {
        var patient = await GetPatientStateAsync(patientProfileId, cancellationToken);

        if (patient is null)
        {
            return [];
        }

        var orders = await dbContext.Orders
            .Include(order => order.Items)
            .Where(order => order.UserId == patient.UserId && !order.IsDeleted)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);

        return orders.Select(order => ToRecord(order, patient.PatientProfileId)).ToList();
    }

    public async Task<CustomerOrderAccessState?> GetCustomerOrderAccessStateAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(user =>
                user.Id == userId &&
                !user.IsDeleted &&
                user.PatientProfile != null &&
                !user.PatientProfile.IsDeleted)
            .Select(user => new CustomerOrderAccessState(
                user.Id,
                user.PatientProfile!.Id,
                (int)user.Status,
                user.Roles.Any(userRole =>
                    userRole.Role != null &&
                    userRole.Role.Name == "Patient" &&
                    !userRole.Role.IsDeleted)))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerOrderSummaryRecord>> GetCustomerOrdersByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Orders
            .AsNoTracking()
            .Where(order => order.UserId == userId && !order.IsDeleted)
            .OrderByDescending(order => order.CreatedAt)
            .Select(order => new CustomerOrderSummaryRecord(
                order.Id,
                order.OrderNumber,
                order.CreatedAt,
                order.Status,
                order.PaymentStatus,
                order.TotalAmount,
                order.Currency))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerOrderDetailRecord?> GetCustomerOrderByIdAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .SingleOrDefaultAsync(
                order =>
                    order.Id == orderId &&
                    order.UserId == userId &&
                    !order.IsDeleted,
                cancellationToken);

        return order is null ? null : ToCustomerDetailRecord(order);
    }

    public async Task<IReadOnlyList<AdminOrderRecord>> GetAdminOrdersAsync(
        AdminOrderFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .Where(order => !order.IsDeleted);

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
            query = query.Where(order => order.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(order => order.CreatedAt <= filter.ToDate.Value);
        }

        var orders = await query
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);
        var patientProfiles = await GetPatientProfilesByUserIdAsync(
            orders.Select(order => order.UserId).Distinct().ToList(),
            cancellationToken);

        return orders
            .Select(order => ToAdminRecord(
                order,
                patientProfiles.TryGetValue(order.UserId, out var patientProfile)
                    ? patientProfile
                    : null))
            .ToList();
    }

    public async Task<AdminOrderRecord?> GetAdminOrderByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .SingleOrDefaultAsync(order => order.Id == orderId && !order.IsDeleted, cancellationToken);

        if (order is null)
        {
            return null;
        }

        var patientProfile = await GetPatientProfileForUserAsync(order.UserId, cancellationToken);

        return ToAdminRecord(order, patientProfile);
    }

    public async Task<AdminOrderRecord?> UpdateAdminOrderStatusAsync(
        Guid orderId,
        OrderStatus status,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders
            .Include(order => order.Items)
            .SingleOrDefaultAsync(order => order.Id == orderId && !order.IsDeleted, cancellationToken);

        if (order is null)
        {
            return null;
        }

        order.Status = status;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var patientProfile = await GetPatientProfileForUserAsync(order.UserId, cancellationToken);

        return ToAdminRecord(order, patientProfile);
    }

    private async Task<Guid> GetPatientProfileIdAsync(Guid patientUserId, CancellationToken cancellationToken)
    {
        return await dbContext.PatientProfiles
            .Where(profile => profile.UserId == patientUserId && !profile.IsDeleted)
            .Select(profile => profile.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, PatientProfile>> GetPatientProfilesByUserIdAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return [];
        }

        var profiles = await dbContext.PatientProfiles
            .AsNoTracking()
            .Include(profile => profile.User)
            .Where(profile => userIds.Contains(profile.UserId))
            .ToListAsync(cancellationToken);

        return profiles
            .GroupBy(profile => profile.UserId)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private async Task<PatientProfile?> GetPatientProfileForUserAsync(
        Guid patientUserId,
        CancellationToken cancellationToken)
    {
        return await dbContext.PatientProfiles
            .AsNoTracking()
            .Include(profile => profile.User)
            .SingleOrDefaultAsync(profile => profile.UserId == patientUserId, cancellationToken);
    }

    private static OrderRecord ToRecord(Order order, Guid patientProfileId)
    {
        return new OrderRecord(
            order.Id,
            patientProfileId,
            order.UserId,
            order.OrderNumber,
            order.Status,
            order.PaymentStatus,
            order.TotalAmount,
            order.Currency,
            order.ShippingAddress,
            order.Items
                .OrderBy(item => item.CreatedAt)
                .Select(ToItemRecord)
                .ToList());
    }

    private static OrderItemRecord ToItemRecord(OrderItem item)
    {
        return new OrderItemRecord(
            item.Id,
            item.ProductId,
            item.ProductName,
            item.Quantity,
            item.UnitPrice,
            item.Subtotal);
    }

    private static AdminOrderRecord ToAdminRecord(Order order, PatientProfile? patientProfile)
    {
        return new AdminOrderRecord(
            order.Id,
            order.OrderNumber,
            patientProfile?.Id ?? Guid.Empty,
            patientProfile?.User?.FullName,
            patientProfile?.User?.Email,
            order.Status,
            order.PaymentStatus,
            order.TotalAmount,
            order.Currency,
            order.ShippingAddress,
            order.CreatedAt,
            order.UpdatedAt,
            order.Items
                .OrderBy(item => item.CreatedAt)
                .Select(ToItemRecord)
                .ToList());
    }

    private static CustomerOrderDetailRecord ToCustomerDetailRecord(Order order)
    {
        return new CustomerOrderDetailRecord(
            order.Id,
            order.OrderNumber,
            order.CreatedAt,
            order.UpdatedAt,
            order.ShippingAddress,
            order.Status,
            order.PaymentStatus,
            order.TotalAmount,
            order.Currency,
            order.Items
                .Where(item => !item.IsDeleted)
                .OrderBy(item => item.CreatedAt)
                .Select(ToItemRecord)
                .ToList());
    }

    private static OrderRepositoryResult Failed(string message, OrderFailureReason reason)
    {
        return new OrderRepositoryResult(false, null, reason, message);
    }
}
