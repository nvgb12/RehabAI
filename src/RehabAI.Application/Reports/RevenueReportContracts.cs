namespace RehabAI.Application.Reports;

public sealed record RevenueReportQuery(
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate);

public sealed record RevenueReportRange(
    DateTimeOffset FromDate,
    DateTimeOffset ToDate);

public sealed record RevenueReportResponse(
    DateTimeOffset FromDate,
    DateTimeOffset ToDate,
    decimal ProductRevenue,
    decimal AppointmentRevenue,
    decimal TotalRevenue,
    int PaidOrderCount,
    int ConfirmedAppointmentCount,
    string Currency);

public sealed record RevenueReportResult(
    bool Succeeded,
    string Message,
    RevenueReportResponse? Report = null,
    RevenueReportFailureReason? FailureReason = null);

public enum RevenueReportFailureReason
{
    Validation = 1
}

public interface IRevenueReportService
{
    Task<RevenueReportResult> GetRevenueReportAsync(
        RevenueReportQuery query,
        CancellationToken cancellationToken = default);
}

public interface IRevenueReportRepository
{
    Task<RevenueReportSnapshot> GetRevenueSnapshotAsync(
        RevenueReportRange range,
        CancellationToken cancellationToken = default);
}

public sealed record RevenueReportSnapshot(
    decimal ProductRevenue,
    decimal AppointmentRevenue,
    int PaidOrderCount,
    int ConfirmedAppointmentCount);
