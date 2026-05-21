using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using RehabAI.Api.Controllers;
using RehabAI.Application.Doctors;
using RehabAI.Application.MedicalServices;
using RehabAI.Application.Orders;
using RehabAI.Application.Products;
using RehabAI.Application.Reports;

namespace RehabAI.UnitTests.Reports;

public class AdminRevenueReportControllerTests
{
    [Fact]
    public async Task GetRevenueReport_WhenDateRangeIsInvalid_ReturnsBadRequest()
    {
        var revenueReportService = new RevenueReportService(new EmptyRevenueReportRepository());
        var controller = new AdminController(
            new FakeDoctorService(),
            new FakeMedicalServiceManager(),
            new FakeProductManager(),
            new FakeOrderService(),
            revenueReportService,
            new FakeHostEnvironment());
        var fromDate = DateTimeOffset.UtcNow;
        var toDate = fromDate.AddDays(-1);

        var response = await controller.GetRevenueReport(fromDate, toDate, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(response);
        Assert.NotNull(badRequest.Value);
    }

    private sealed class EmptyRevenueReportRepository : IRevenueReportRepository
    {
        public Task<RevenueReportSnapshot> GetRevenueSnapshotAsync(
            RevenueReportRange range,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new RevenueReportSnapshot(0m, 0m, 0, 0));
        }
    }

    private sealed class FakeDoctorService : IDoctorService
    {
        public Task<CreateDoctorResult> CreateDoctorAsync(
            CreateDoctorCommand command,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CreateDoctorResult(false, "Not used."));
        }

        public Task<IReadOnlyList<AdminDoctorResponse>> GetAdminDoctorsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AdminDoctorResponse>>([]);
        }

        public Task ResendInvitationAsync(Guid doctorProfileId, Guid adminUserId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMedicalServiceManager : IMedicalServiceManager
    {
        public Task<IReadOnlyList<MedicalServiceResponse>> GetActiveMedicalServicesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<MedicalServiceResponse>>([]);
        }

        public Task<MedicalServiceResponse?> GetActiveMedicalServiceByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<MedicalServiceResponse?>(null);
        }

        public Task<MedicalServiceResult> CreateAsync(UpsertMedicalServiceCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new MedicalServiceResult(false, "Not used."));
        }

        public Task<MedicalServiceResult> UpdateAsync(Guid id, UpsertMedicalServiceCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new MedicalServiceResult(false, "Not used."));
        }

        public Task<MedicalServiceResult> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new MedicalServiceResult(false, "Not used."));
        }
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

        public Task<IReadOnlyList<OrderResponse>> GetPatientOrdersAsync(Guid patientProfileId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<OrderResponse>>([]);
        }

        public Task<CustomerOrderListResult> GetMyOrdersAsync(
            Guid currentUserId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CustomerOrderListResult(true, "Not used.", []));
        }

        public Task<CustomerOrderDetailResult> GetMyOrderByIdAsync(
            Guid currentUserId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CustomerOrderDetailResult(false, "Not used."));
        }

        public Task<AdminOrderListResult> GetAdminOrdersAsync(AdminOrderQuery query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AdminOrderListResult(true, "Not used.", []));
        }

        public Task<AdminOrderDetailResponse?> GetAdminOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
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

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "RehabAI.UnitTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
