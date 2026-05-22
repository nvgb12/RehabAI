using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using RehabAI.Api.Contracts.MedicalServices;
using RehabAI.Api.Controllers;
using RehabAI.Application.Doctors;
using RehabAI.Application.MedicalServices;
using RehabAI.Application.Orders;
using RehabAI.Application.Products;
using RehabAI.Application.Reports;

namespace RehabAI.UnitTests.MedicalServices;

public class AdminMedicalServiceControllerTests
{
    [Fact]
    public async Task CreateMedicalService_WhenIsActiveIsNotProvided_DefaultsToActive()
    {
        var medicalServiceManager = new FakeMedicalServiceManager();
        var controller = new AdminController(
            new FakeDoctorService(),
            medicalServiceManager,
            new FakeProductManager(),
            new FakeOrderService(),
            new FakeRevenueReportService(),
            new FakeHostEnvironment());

        var response = await controller.CreateMedicalService(
            new CreateMedicalServiceRequest(
                "Initial Rehabilitation Consultation",
                "First consultation for rehabilitation planning.",
                60,
                300000,
                "VND",
                null,
                false,
                null),
            CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(response);
        Assert.True(medicalServiceManager.LastCreateCommand!.IsActive);
    }

    [Fact]
    public async Task UpdateMedicalService_WhenIsActiveIsFalse_RespectsProvidedValue()
    {
        var medicalServiceManager = new FakeMedicalServiceManager();
        var controller = new AdminController(
            new FakeDoctorService(),
            medicalServiceManager,
            new FakeProductManager(),
            new FakeOrderService(),
            new FakeRevenueReportService(),
            new FakeHostEnvironment());

        var response = await controller.UpdateMedicalService(
            Guid.NewGuid(),
            new UpsertMedicalServiceRequest(
                "Initial Rehabilitation Consultation",
                "First consultation for rehabilitation planning.",
                60,
                300000,
                "VND",
                false,
                false,
                null),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(response);
        Assert.False(medicalServiceManager.LastUpdateCommand!.IsActive);
    }

    private sealed class FakeMedicalServiceManager : IMedicalServiceManager
    {
        public UpsertMedicalServiceCommand? LastCreateCommand { get; private set; }
        public UpsertMedicalServiceCommand? LastUpdateCommand { get; private set; }

        public Task<IReadOnlyList<MedicalServiceResponse>> GetActiveMedicalServicesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<MedicalServiceResponse>>([]);
        }

        public Task<MedicalServiceResponse?> GetActiveMedicalServiceByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<MedicalServiceResponse?>(null);
        }

        public Task<MedicalServiceResult> CreateAsync(
            UpsertMedicalServiceCommand command,
            CancellationToken cancellationToken = default)
        {
            LastCreateCommand = command;

            return Task.FromResult(new MedicalServiceResult(
                true,
                "Medical service created successfully.",
                ToResponse(command)));
        }

        public Task<MedicalServiceResult> UpdateAsync(
            Guid id,
            UpsertMedicalServiceCommand command,
            CancellationToken cancellationToken = default)
        {
            LastUpdateCommand = command;

            return Task.FromResult(new MedicalServiceResult(
                true,
                "Medical service updated successfully.",
                ToResponse(command, id)));
        }

        public Task<MedicalServiceResult> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new MedicalServiceResult(true, "Medical service deleted successfully."));
        }

        private static MedicalServiceResponse ToResponse(UpsertMedicalServiceCommand command, Guid? id = null)
        {
            return new MedicalServiceResponse(
                id ?? Guid.NewGuid(),
                command.Name,
                command.Description,
                command.DurationMinutes,
                command.Price,
                command.Currency ?? "VND",
                command.IsActive,
                command.NoShowFeeEnabled,
                command.NoShowFeeAmount);
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

        public Task<AdminDoctorResponse?> GetAdminDoctorByIdAsync(Guid doctorProfileId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AdminDoctorResponse?>(null);
        }

        public Task<AdminDoctorPublicProfileReviewResult> ApprovePublicProfileAsync(
            Guid doctorProfileId,
            Guid adminUserId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AdminDoctorPublicProfileReviewResult(false, "Not used."));
        }

        public Task<AdminDoctorPublicProfileReviewResult> RejectPublicProfileAsync(
            Guid doctorProfileId,
            Guid adminUserId,
            string rejectionReason,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AdminDoctorPublicProfileReviewResult(false, "Not used."));
        }

        public Task ResendInvitationAsync(Guid doctorProfileId, Guid adminUserId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
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
            return Task.FromResult(new CustomerOrderListResult(true, "Not used.", []));
        }

        public Task<CustomerOrderDetailResult> GetMyOrderByIdAsync(
            Guid currentUserId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CustomerOrderDetailResult(false, "Not used."));
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

    private sealed class FakeRevenueReportService : IRevenueReportService
    {
        public Task<RevenueReportResult> GetRevenueReportAsync(
            RevenueReportQuery query,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new RevenueReportResult(false, "Not used."));
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
