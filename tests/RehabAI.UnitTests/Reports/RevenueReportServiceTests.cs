using RehabAI.Application.Reports;
using RehabAI.Domain.Enums;

namespace RehabAI.UnitTests.Reports;

public class RevenueReportServiceTests
{
    [Fact]
    public async Task GetRevenueReportAsync_WhenRangeIsValid_ReturnsRevenueSummary()
    {
        var repository = new FakeRevenueReportRepository();
        var service = new RevenueReportService(repository);
        var fromDate = DateTimeOffset.UtcNow.AddDays(-1);
        var toDate = DateTimeOffset.UtcNow.AddDays(1);
        repository.AddOrder(200000m, OrderStatus.Processing, PaymentStatus.Paid, fromDate.AddHours(1));
        repository.AddAppointment(300000m, AppointmentStatus.Confirmed, fromDate.AddHours(2));

        var result = await service.GetRevenueReportAsync(new RevenueReportQuery(fromDate, toDate));

        Assert.True(result.Succeeded);
        Assert.Equal(200000m, result.Report!.ProductRevenue);
        Assert.Equal(300000m, result.Report.AppointmentRevenue);
        Assert.Equal(500000m, result.Report.TotalRevenue);
        Assert.Equal(1, result.Report.PaidOrderCount);
        Assert.Equal(1, result.Report.ConfirmedAppointmentCount);
        Assert.Equal("VND", result.Report.Currency);
    }

    [Fact]
    public async Task GetRevenueReportAsync_WhenDateRangeIsInvalid_ReturnsValidationFailure()
    {
        var repository = new FakeRevenueReportRepository();
        var service = new RevenueReportService(repository);
        var fromDate = DateTimeOffset.UtcNow;
        var toDate = fromDate.AddDays(-1);

        var result = await service.GetRevenueReportAsync(new RevenueReportQuery(fromDate, toDate));

        Assert.False(result.Succeeded);
        Assert.Equal(RevenueReportFailureReason.Validation, result.FailureReason);
        Assert.Equal("fromDate must be before or equal to toDate.", result.Message);
    }

    [Fact]
    public async Task GetRevenueReportAsync_ExcludesPendingOrders()
    {
        var repository = new FakeRevenueReportRepository();
        var service = new RevenueReportService(repository);
        var fromDate = DateTimeOffset.UtcNow.AddDays(-1);
        var toDate = DateTimeOffset.UtcNow.AddDays(1);
        repository.AddOrder(200000m, OrderStatus.Processing, PaymentStatus.Paid, fromDate.AddHours(1));
        repository.AddOrder(900000m, OrderStatus.PendingPayment, PaymentStatus.Pending, fromDate.AddHours(2));

        var result = await service.GetRevenueReportAsync(new RevenueReportQuery(fromDate, toDate));

        Assert.True(result.Succeeded);
        Assert.Equal(200000m, result.Report!.ProductRevenue);
        Assert.Equal(1, result.Report.PaidOrderCount);
    }

    [Fact]
    public async Task GetRevenueReportAsync_ExcludesCancelledAndDeletedRecords()
    {
        var repository = new FakeRevenueReportRepository();
        var service = new RevenueReportService(repository);
        var fromDate = DateTimeOffset.UtcNow.AddDays(-1);
        var toDate = DateTimeOffset.UtcNow.AddDays(1);
        repository.AddOrder(200000m, OrderStatus.Processing, PaymentStatus.Paid, fromDate.AddHours(1));
        repository.AddOrder(900000m, OrderStatus.Cancelled, PaymentStatus.Paid, fromDate.AddHours(2));
        repository.AddOrder(800000m, OrderStatus.Processing, PaymentStatus.Paid, fromDate.AddHours(3), isDeleted: true);
        repository.AddAppointment(300000m, AppointmentStatus.Confirmed, fromDate.AddHours(4));
        repository.AddAppointment(700000m, AppointmentStatus.Cancelled, fromDate.AddHours(5));
        repository.AddAppointment(600000m, AppointmentStatus.Confirmed, fromDate.AddHours(6), isDeleted: true);

        var result = await service.GetRevenueReportAsync(new RevenueReportQuery(fromDate, toDate));

        Assert.True(result.Succeeded);
        Assert.Equal(200000m, result.Report!.ProductRevenue);
        Assert.Equal(300000m, result.Report.AppointmentRevenue);
        Assert.Equal(1, result.Report.PaidOrderCount);
        Assert.Equal(1, result.Report.ConfirmedAppointmentCount);
    }

    [Fact]
    public async Task GetRevenueReportAsync_CalculatesProductRevenueCorrectly()
    {
        var repository = new FakeRevenueReportRepository();
        var service = new RevenueReportService(repository);
        var fromDate = DateTimeOffset.UtcNow.AddDays(-1);
        var toDate = DateTimeOffset.UtcNow.AddDays(1);
        repository.AddOrder(120000m, OrderStatus.Processing, PaymentStatus.Paid, fromDate.AddHours(1));
        repository.AddOrder(180000m, OrderStatus.Completed, PaymentStatus.Paid, fromDate.AddHours(2));

        var result = await service.GetRevenueReportAsync(new RevenueReportQuery(fromDate, toDate));

        Assert.True(result.Succeeded);
        Assert.Equal(300000m, result.Report!.ProductRevenue);
        Assert.Equal(2, result.Report.PaidOrderCount);
    }

    private sealed class FakeRevenueReportRepository : IRevenueReportRepository
    {
        private readonly List<ReportOrder> orders = [];
        private readonly List<ReportAppointment> appointments = [];

        public void AddOrder(
            decimal totalAmount,
            OrderStatus status,
            PaymentStatus paymentStatus,
            DateTimeOffset createdAt,
            bool isDeleted = false)
        {
            orders.Add(new ReportOrder(totalAmount, status, paymentStatus, createdAt, isDeleted));
        }

        public void AddAppointment(
            decimal servicePrice,
            AppointmentStatus status,
            DateTimeOffset createdAt,
            bool isDeleted = false)
        {
            appointments.Add(new ReportAppointment(servicePrice, status, createdAt, isDeleted));
        }

        public Task<RevenueReportSnapshot> GetRevenueSnapshotAsync(
            RevenueReportRange range,
            CancellationToken cancellationToken = default)
        {
            var paidOrders = orders
                .Where(order =>
                    !order.IsDeleted &&
                    order.CreatedAt >= range.FromDate &&
                    order.CreatedAt <= range.ToDate &&
                    order.PaymentStatus == PaymentStatus.Paid &&
                    order.Status is not (OrderStatus.PendingPayment or OrderStatus.Cancelled or OrderStatus.Refunded))
                .ToList();
            var confirmedAppointments = appointments
                .Where(appointment =>
                    !appointment.IsDeleted &&
                    appointment.CreatedAt >= range.FromDate &&
                    appointment.CreatedAt <= range.ToDate &&
                    appointment.Status is AppointmentStatus.Confirmed or AppointmentStatus.Completed)
                .ToList();

            return Task.FromResult(new RevenueReportSnapshot(
                paidOrders.Sum(order => order.TotalAmount),
                confirmedAppointments.Sum(appointment => appointment.ServicePrice),
                paidOrders.Count,
                confirmedAppointments.Count));
        }

        private sealed record ReportOrder(
            decimal TotalAmount,
            OrderStatus Status,
            PaymentStatus PaymentStatus,
            DateTimeOffset CreatedAt,
            bool IsDeleted);

        private sealed record ReportAppointment(
            decimal ServicePrice,
            AppointmentStatus Status,
            DateTimeOffset CreatedAt,
            bool IsDeleted);
    }
}
