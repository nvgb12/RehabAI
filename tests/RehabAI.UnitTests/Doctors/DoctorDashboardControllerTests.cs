using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RehabAI.Api.Authorization;
using RehabAI.Api.Contracts.Doctors;
using RehabAI.Api.Controllers;
using RehabAI.Application.Doctors;
using RehabAI.Application.DoctorSchedules;

namespace RehabAI.UnitTests.Doctors;

public class DoctorDashboardControllerTests
{
    [Fact]
    public async Task GetMyProfile_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var controller = CreateController(new FakeDoctorDashboardService());

        var response = await controller.GetMyProfile(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(response);
    }

    [Fact]
    public async Task GetMyProfile_WhenDoctorExists_ReturnsOwnProfile()
    {
        var userId = Guid.NewGuid();
        var service = new FakeDoctorDashboardService
        {
            Profile = new DoctorProfileResponse(
                Guid.NewGuid(),
                userId,
                "Dr Stroke Rehab",
                "doctor@test.com",
                "0912345678",
                "Active",
                true,
                Guid.NewGuid(),
                "Stroke Rehabilitation",
                "Post-stroke rehab doctor.",
                null,
                true,
                "Draft",
                null,
                null,
                null,
                null,
                null,
                null,
                DateTimeOffset.UtcNow.AddDays(-1),
                null)
        };
        var controller = CreateController(service, userId);

        var response = await controller.GetMyProfile(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response);
        var profile = Assert.IsType<DoctorProfileResponse>(ok.Value);
        Assert.Equal(userId, profile.UserId);
    }

    [Fact]
    public async Task GetMyAppointment_WhenNotOwned_ReturnsNotFound()
    {
        var controller = CreateController(new FakeDoctorDashboardService(), Guid.NewGuid());

        var response = await controller.GetMyAppointment(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(response);
    }

    [Fact]
    public async Task UploadMyAvatar_WhenFileMissing_ReturnsBadRequest()
    {
        var controller = CreateController(new FakeDoctorDashboardService(), Guid.NewGuid());

        var response = await controller.UploadMyAvatar(
            new UploadDoctorAvatarRequest(),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(response);
    }

    private static DoctorsController CreateController(
        FakeDoctorDashboardService doctorDashboardService,
        Guid? userId = null)
    {
        var controller = new DoctorsController(
            new FakePublicDoctorListingService(),
            doctorDashboardService,
            new FakeDoctorScheduleSlotService(),
            new FakeEndpointAccessService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        if (userId.HasValue)
        {
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", userId.Value.ToString())],
                "TestAuth"));
        }

        return controller;
    }

    private sealed class FakeDoctorDashboardService : IDoctorDashboardService
    {
        public DoctorProfileResponse? Profile { get; init; }

        public Task<DoctorProfileResponse?> GetOwnProfileAsync(
            Guid currentUserId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Profile);
        }

        public Task<DoctorProfileResult> UpdateOwnProfileAsync(
            Guid currentUserId,
            UpdateDoctorProfileCommand command,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DoctorProfileResult(true, "Updated.", Profile));
        }

        public Task<IReadOnlyList<DoctorAppointmentResponse>> GetOwnAppointmentsAsync(
            Guid currentUserId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DoctorAppointmentResponse>>([]);
        }

        public Task<DoctorAppointmentResponse?> GetOwnAppointmentByIdAsync(
            Guid currentUserId,
            Guid appointmentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DoctorAppointmentResponse?>(null);
        }

        public Task<IReadOnlyList<DoctorAppointmentResponse>> GetOwnAppointmentRequestsAsync(
            Guid currentUserId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DoctorAppointmentResponse>>([]);
        }

        public Task<DoctorAppointmentActionResult> AcceptAppointmentRequestAsync(
            Guid currentUserId,
            Guid appointmentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DoctorAppointmentActionResult(false, "Not used."));
        }

        public Task<DoctorAppointmentActionResult> RejectAppointmentRequestAsync(
            Guid currentUserId,
            Guid appointmentId,
            string? rejectionReason,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DoctorAppointmentActionResult(false, "Not used."));
        }

        public Task<DoctorDashboardSummaryResponse?> GetDashboardAsync(
            Guid currentUserId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DoctorDashboardSummaryResponse?>(null);
        }

        public Task<DoctorAvatarUploadResult> UploadAvatarAsync(
            UploadDoctorAvatarCommand command,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DoctorAvatarUploadResult(true, "Uploaded.", "/uploads/doctor-avatars/test.png"));
        }

        public Task<DoctorPublicProfileSubmitResult> SubmitPublicProfileForReviewAsync(
            Guid currentUserId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DoctorPublicProfileSubmitResult(false, "Not used."));
        }
    }

    private sealed class FakePublicDoctorListingService : IPublicDoctorListingService
    {
        public Task<PublicDoctorSearchResult> SearchAsync(
            PublicDoctorSearchQuery query,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PublicDoctorSearchResult(true, "OK", []));
        }

        public Task<PublicDoctorSummaryResponse?> GetByIdAsync(
            Guid doctorProfileId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PublicDoctorSummaryResponse?>(null);
        }
    }

    private sealed class FakeDoctorScheduleSlotService : IDoctorScheduleSlotService
    {
        public Task<IReadOnlyList<DoctorScheduleSlotResponse>> GetDoctorSlotsAsync(
            Guid doctorProfileId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DoctorScheduleSlotResponse>>([]);
        }

        public Task<IReadOnlyList<DoctorScheduleSlotResponse>> GetAvailableSlotsAsync(
            Guid doctorProfileId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DoctorScheduleSlotResponse>>([]);
        }

        public Task<DoctorScheduleSlotResult> CreateAsync(
            CreateDoctorScheduleSlotCommand command,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DoctorScheduleSlotResult(false, "Not used."));
        }

        public Task<DoctorScheduleSlotResult> UpdateAsync(
            UpdateDoctorScheduleSlotCommand command,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DoctorScheduleSlotResult(false, "Not used."));
        }

        public Task<DoctorScheduleSlotResult> DisableAsync(
            DisableDoctorScheduleSlotCommand command,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DoctorScheduleSlotResult(false, "Not used."));
        }
    }

    private sealed class FakeEndpointAccessService : IEndpointAccessService
    {
        public Task<bool> PatientProfileBelongsToUserAsync(
            Guid userId,
            Guid patientProfileId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<Guid?> GetPatientProfileIdForUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Guid?>(null);
        }

        public Task<bool> AppointmentBelongsToUserAsync(
            Guid userId,
            Guid appointmentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> OrderBelongsToUserAsync(
            Guid userId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> CanManageDoctorProfileAsync(
            Guid userId,
            Guid doctorProfileId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }
    }
}
