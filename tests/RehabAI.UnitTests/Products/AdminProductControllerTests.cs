using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using RehabAI.Api.Contracts.Products;
using RehabAI.Api.Controllers;
using RehabAI.Application.Doctors;
using RehabAI.Application.MedicalServices;
using RehabAI.Application.Orders;
using RehabAI.Application.Products;
using RehabAI.Application.Reports;

namespace RehabAI.UnitTests.Products;

public class AdminProductControllerTests
{
    [Fact]
    public async Task CreateProduct_WhenIsActiveIsNotProvided_DefaultsToActive()
    {
        var productManager = new FakeProductManager();
        var controller = new AdminController(
            new FakeDoctorService(),
            new FakeMedicalServiceManager(),
            productManager,
            new FakeOrderService(),
            new FakeRevenueReportService(),
            new FakeHostEnvironment());

        var response = await controller.CreateProduct(
            new CreateProductRequest(
                "Stroke Mobility Aid",
                "Support product for stroke mobility training.",
                Guid.NewGuid(),
                450000,
                null,
                10,
                null,
                null),
            CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(response);
        Assert.True(productManager.LastCreateCommand!.IsActive);
    }

    [Fact]
    public async Task UpdateProduct_WhenIsActiveIsFalse_RespectsProvidedValue()
    {
        var productManager = new FakeProductManager();
        var controller = new AdminController(
            new FakeDoctorService(),
            new FakeMedicalServiceManager(),
            productManager,
            new FakeOrderService(),
            new FakeRevenueReportService(),
            new FakeHostEnvironment());

        var response = await controller.UpdateProduct(
            Guid.NewGuid(),
            new UpdateProductRequest(
                "Stroke Mobility Aid",
                "Support product for stroke mobility training.",
                Guid.NewGuid(),
                450000,
                "VND",
                10,
                null,
                false),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(response);
        Assert.False(productManager.LastUpdateCommand!.IsActive);
    }

    private sealed class FakeProductManager : IProductManager
    {
        public UpsertProductCommand? LastCreateCommand { get; private set; }
        public UpsertProductCommand? LastUpdateCommand { get; private set; }

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

        public Task<ProductResult> CreateAsync(
            UpsertProductCommand command,
            CancellationToken cancellationToken = default)
        {
            LastCreateCommand = command;

            return Task.FromResult(new ProductResult(
                true,
                "Product created successfully.",
                ToResponse(command)));
        }

        public Task<ProductResult> UpdateAsync(
            Guid id,
            UpsertProductCommand command,
            CancellationToken cancellationToken = default)
        {
            LastUpdateCommand = command;

            return Task.FromResult(new ProductResult(
                true,
                "Product updated successfully.",
                ToResponse(command, id)));
        }

        public Task<ProductResult> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ProductResult(true, "Product deleted successfully."));
        }

        private static ProductResponse ToResponse(UpsertProductCommand command, Guid? id = null)
        {
            return new ProductResponse(
                id ?? Guid.NewGuid(),
                command.CategoryId,
                "Stroke Rehabilitation Equipment",
                command.Name,
                command.Name.ToLowerInvariant().Replace(' ', '-'),
                command.Description,
                command.Price,
                command.Currency ?? "VND",
                command.StockQuantity,
                command.ImageUrl,
                command.IsActive);
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
