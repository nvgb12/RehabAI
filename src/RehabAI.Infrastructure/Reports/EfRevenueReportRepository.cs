using Microsoft.EntityFrameworkCore;
using RehabAI.Application.Reports;
using RehabAI.Domain.Enums;
using RehabAI.Infrastructure.Database;

namespace RehabAI.Infrastructure.Reports;

public sealed class EfRevenueReportRepository(AppDbContext dbContext) : IRevenueReportRepository
{
    public async Task<RevenueReportSnapshot> GetRevenueSnapshotAsync(
        RevenueReportRange range,
        CancellationToken cancellationToken = default)
    {
        var paidOrderQuery = dbContext.Orders
            .AsNoTracking()
            .Where(order =>
                !order.IsDeleted &&
                order.CreatedAt >= range.FromDate &&
                order.CreatedAt <= range.ToDate &&
                order.PaymentStatus == PaymentStatus.Paid &&
                order.Status != OrderStatus.PendingPayment &&
                order.Status != OrderStatus.Cancelled &&
                order.Status != OrderStatus.Refunded);

        var productRevenue = await paidOrderQuery
            .SumAsync(order => (decimal?)order.TotalAmount, cancellationToken) ?? 0m;
        var paidOrderCount = await paidOrderQuery.CountAsync(cancellationToken);

        var confirmedAppointmentQuery = dbContext.Appointments
            .AsNoTracking()
            .Where(appointment =>
                !appointment.IsDeleted &&
                appointment.CreatedAt >= range.FromDate &&
                appointment.CreatedAt <= range.ToDate &&
                (appointment.Status == AppointmentStatus.Confirmed ||
                    appointment.Status == AppointmentStatus.Completed));

        var appointmentRevenue = await confirmedAppointmentQuery
            .Join(
                dbContext.MedicalServices.AsNoTracking(),
                appointment => appointment.MedicalServiceId,
                medicalService => medicalService.Id,
                (_, medicalService) => (decimal?)medicalService.Price)
            .SumAsync(cancellationToken) ?? 0m;
        var confirmedAppointmentCount = await confirmedAppointmentQuery.CountAsync(cancellationToken);

        return new RevenueReportSnapshot(
            productRevenue,
            appointmentRevenue,
            paidOrderCount,
            confirmedAppointmentCount);
    }
}
