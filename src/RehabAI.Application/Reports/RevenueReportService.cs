namespace RehabAI.Application.Reports;

public sealed class RevenueReportService(IRevenueReportRepository repository) : IRevenueReportService
{
    private const string DefaultCurrency = "VND";

    public async Task<RevenueReportResult> GetRevenueReportAsync(
        RevenueReportQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!query.FromDate.HasValue)
        {
            return Failed("fromDate is required.");
        }

        if (!query.ToDate.HasValue)
        {
            return Failed("toDate is required.");
        }

        if (query.FromDate.Value > query.ToDate.Value)
        {
            return Failed("fromDate must be before or equal to toDate.");
        }

        var range = new RevenueReportRange(query.FromDate.Value, query.ToDate.Value);
        var snapshot = await repository.GetRevenueSnapshotAsync(range, cancellationToken);
        var totalRevenue = snapshot.ProductRevenue + snapshot.AppointmentRevenue;

        return new RevenueReportResult(
            true,
            "Revenue report retrieved successfully.",
            new RevenueReportResponse(
                range.FromDate,
                range.ToDate,
                snapshot.ProductRevenue,
                snapshot.AppointmentRevenue,
                totalRevenue,
                snapshot.PaidOrderCount,
                snapshot.ConfirmedAppointmentCount,
                DefaultCurrency));
    }

    private static RevenueReportResult Failed(string message)
    {
        return new RevenueReportResult(
            false,
            message,
            FailureReason: RevenueReportFailureReason.Validation);
    }
}
