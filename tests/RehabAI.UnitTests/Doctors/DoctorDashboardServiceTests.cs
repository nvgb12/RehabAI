using RehabAI.Application.Doctors;
using RehabAI.Domain.Enums;

namespace RehabAI.UnitTests.Doctors;

public class DoctorDashboardServiceTests
{
    [Fact]
    public async Task GetOwnProfileAsync_WhenDoctorExists_ReturnsSafeProfile()
    {
        var repository = new FakeDoctorDashboardRepository();
        var service = new DoctorDashboardService(repository, new FakeDoctorAvatarStorage());

        var profile = await service.GetOwnProfileAsync(repository.UserId);

        Assert.NotNull(profile);
        Assert.Equal(repository.DoctorProfileId, profile.DoctorProfileId);
        Assert.Equal(repository.UserId, profile.UserId);
        Assert.Equal("Dr Stroke Rehab", profile.FullName);
        Assert.Equal("doctor@test.com", profile.Email);
        Assert.Equal("Active", profile.Status);
        Assert.Null(profile.YearsOfExperience);
    }

    [Fact]
    public async Task GetOwnAppointmentsAsync_ReturnsOnlyRepositoryOwnedAppointments()
    {
        var repository = new FakeDoctorDashboardRepository();
        var service = new DoctorDashboardService(repository, new FakeDoctorAvatarStorage());

        var appointments = await service.GetOwnAppointmentsAsync(repository.UserId);

        var appointment = Assert.Single(
            appointments,
            appointment => appointment.AppointmentId == repository.AppointmentId);
        Assert.Equal(repository.AppointmentId, appointment.AppointmentId);
        Assert.Equal("Stroke Rehab Patient", appointment.PatientName);
    }

    [Fact]
    public async Task GetOwnAppointmentRequestsAsync_ReturnsRequestedAppointments()
    {
        var repository = new FakeDoctorDashboardRepository();
        var service = new DoctorDashboardService(repository, new FakeDoctorAvatarStorage());

        var appointments = await service.GetOwnAppointmentRequestsAsync(repository.UserId);

        var appointment = Assert.Single(appointments);
        Assert.Equal(repository.RequestedAppointmentId, appointment.AppointmentId);
        Assert.Equal("Requested", appointment.Status);
    }

    [Fact]
    public async Task AcceptAppointmentRequestAsync_WhenRequested_ReturnsPendingPaymentAppointment()
    {
        var repository = new FakeDoctorDashboardRepository();
        var service = new DoctorDashboardService(repository, new FakeDoctorAvatarStorage());

        var result = await service.AcceptAppointmentRequestAsync(
            repository.UserId,
            repository.RequestedAppointmentId);

        Assert.True(result.Succeeded);
        Assert.Equal("PendingPayment", result.Appointment!.Status);
    }

    [Fact]
    public async Task RejectAppointmentRequestAsync_WhenRequested_ReturnsRejectedAppointment()
    {
        var repository = new FakeDoctorDashboardRepository();
        var service = new DoctorDashboardService(repository, new FakeDoctorAvatarStorage());

        var result = await service.RejectAppointmentRequestAsync(
            repository.UserId,
            repository.RequestedAppointmentId,
            "Doctor is unavailable for the requested stroke therapy time.");

        Assert.True(result.Succeeded);
        Assert.Equal("Rejected", result.Appointment!.Status);
    }

    [Fact]
    public async Task RejectAppointmentRequestAsync_WhenReasonIsMissing_ReturnsValidationFailure()
    {
        var repository = new FakeDoctorDashboardRepository();
        var service = new DoctorDashboardService(repository, new FakeDoctorAvatarStorage());

        var result = await service.RejectAppointmentRequestAsync(
            repository.UserId,
            repository.RequestedAppointmentId,
            " ");

        Assert.False(result.Succeeded);
        Assert.Equal(DoctorDashboardFailureReason.Validation, result.FailureReason);
    }

    [Fact]
    public async Task AcceptAppointmentRequestAsync_WhenAppointmentBelongsToAnotherDoctor_ReturnsNotFound()
    {
        var repository = new FakeDoctorDashboardRepository();
        var service = new DoctorDashboardService(repository, new FakeDoctorAvatarStorage());

        var result = await service.AcceptAppointmentRequestAsync(
            Guid.NewGuid(),
            repository.RequestedAppointmentId);

        Assert.False(result.Succeeded);
        Assert.Equal(DoctorDashboardFailureReason.AppointmentNotFound, result.FailureReason);
    }

    [Fact]
    public async Task GetOwnAppointmentByIdAsync_WhenAppointmentIsNotOwned_ReturnsNull()
    {
        var repository = new FakeDoctorDashboardRepository();
        var service = new DoctorDashboardService(repository, new FakeDoctorAvatarStorage());

        var appointment = await service.GetOwnAppointmentByIdAsync(repository.UserId, Guid.NewGuid());

        Assert.Null(appointment);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsSummaryCounts()
    {
        var repository = new FakeDoctorDashboardRepository();
        var service = new DoctorDashboardService(repository, new FakeDoctorAvatarStorage());

        var dashboard = await service.GetDashboardAsync(repository.UserId);

        Assert.NotNull(dashboard);
        Assert.Equal(repository.DoctorProfileId, dashboard.DoctorProfileId);
        Assert.Equal(2, dashboard.UpcomingAppointmentCount);
        Assert.Equal(1, dashboard.TodayAppointmentCount);
        Assert.Equal(3, dashboard.AvailableSlotCount);
        Assert.Equal(4, dashboard.BookedSlotCount);
        Assert.NotNull(dashboard.NextAppointment);
    }

    [Fact]
    public async Task UploadAvatarAsync_WhenValidImage_SavesAvatarUrl()
    {
        var repository = new FakeDoctorDashboardRepository();
        var storage = new FakeDoctorAvatarStorage();
        var service = new DoctorDashboardService(repository, storage);

        var result = await service.UploadAvatarAsync(new UploadDoctorAvatarCommand(
            repository.UserId,
            "avatar.webp",
            "image/webp",
            128,
            new MemoryStream([1, 2, 3])));

        Assert.True(result.Succeeded);
        Assert.Equal("/uploads/doctor-avatars/test.webp", result.AvatarUrl);
        Assert.Equal("/uploads/doctor-avatars/test.webp", repository.AvatarUrl);
        Assert.Equal(".webp", storage.Extension);
    }

    [Fact]
    public async Task UploadAvatarAsync_WhenInvalidExtension_ReturnsValidationFailure()
    {
        var repository = new FakeDoctorDashboardRepository();
        var service = new DoctorDashboardService(repository, new FakeDoctorAvatarStorage());

        var result = await service.UploadAvatarAsync(new UploadDoctorAvatarCommand(
            repository.UserId,
            "avatar.gif",
            "image/gif",
            128,
            new MemoryStream([1, 2, 3])));

        Assert.False(result.Succeeded);
        Assert.Equal(DoctorDashboardFailureReason.Validation, result.FailureReason);
    }

    [Fact]
    public async Task UploadAvatarAsync_WhenFileIsTooLarge_ReturnsFileTooLarge()
    {
        var repository = new FakeDoctorDashboardRepository();
        var service = new DoctorDashboardService(repository, new FakeDoctorAvatarStorage());

        var result = await service.UploadAvatarAsync(new UploadDoctorAvatarCommand(
            repository.UserId,
            "avatar.png",
            "image/png",
            2 * 1024 * 1024 + 1,
            new MemoryStream([1, 2, 3])));

        Assert.False(result.Succeeded);
        Assert.Equal(DoctorDashboardFailureReason.FileTooLarge, result.FailureReason);
    }

    [Fact]
    public async Task SubmitPublicProfileForReviewAsync_WhenProfileIsReady_SubmitsProfile()
    {
        var repository = new FakeDoctorDashboardRepository();
        var service = new DoctorDashboardService(repository, new FakeDoctorAvatarStorage());
        await service.UploadAvatarAsync(new UploadDoctorAvatarCommand(
            repository.UserId,
            "avatar.png",
            "image/png",
            128,
            new MemoryStream([1, 2, 3])));

        var result = await service.SubmitPublicProfileForReviewAsync(repository.UserId);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Profile);
        Assert.Equal("Submitted", result.Profile.PublicProfileReviewStatus);
        Assert.False(result.Profile.PublicProfileApproved);
    }

    [Fact]
    public async Task SubmitPublicProfileForReviewAsync_WhenReadinessIsMissing_ReturnsValidationFailure()
    {
        var repository = new FakeDoctorDashboardRepository();
        var service = new DoctorDashboardService(repository, new FakeDoctorAvatarStorage());

        var result = await service.SubmitPublicProfileForReviewAsync(repository.UserId);

        Assert.False(result.Succeeded);
        Assert.Equal(DoctorDashboardFailureReason.Validation, result.FailureReason);
        Assert.Contains("Avatar/profile image", result.MissingReadinessItems!);
    }

    private sealed class FakeDoctorDashboardRepository : IDoctorDashboardRepository
    {
        private readonly DoctorAppointmentRecord appointment;
        private DoctorAppointmentRecord requestedAppointment;
        private DoctorProfileRecord profile;

        public FakeDoctorDashboardRepository()
        {
            UserId = Guid.NewGuid();
            DoctorProfileId = Guid.NewGuid();
            SpecialtyId = Guid.NewGuid();
            PatientProfileId = Guid.NewGuid();
            AppointmentId = Guid.NewGuid();
            RequestedAppointmentId = Guid.NewGuid();
            var start = DateTimeOffset.UtcNow.AddHours(2);
            profile = new DoctorProfileRecord(
                DoctorProfileId,
                UserId,
                "Dr Stroke Rehab",
                "doctor@test.com",
                "0912345678",
                AccountStatus.Active,
                true,
                SpecialtyId,
                "Stroke Rehabilitation",
                "Post-stroke rehabilitation specialist.",
                true,
                DoctorProfileReviewStatus.Draft,
                null,
                null,
                null,
                null,
                null,
                DateTimeOffset.UtcNow.AddDays(-3),
                null);
            appointment = new DoctorAppointmentRecord(
                AppointmentId,
                PatientProfileId,
                "Stroke Rehab Patient",
                Guid.NewGuid(),
                "Post-stroke rehabilitation consultation",
                Guid.NewGuid(),
                start,
                start.AddHours(1),
                AppointmentStatus.Confirmed,
                PaymentStatus.Paid,
                "Stroke mobility assessment",
                DateTimeOffset.UtcNow.AddDays(-1));
            requestedAppointment = appointment with
            {
                AppointmentId = RequestedAppointmentId,
                DoctorScheduleSlotId = null,
                Status = AppointmentStatus.Requested,
                PaymentStatus = null,
                Notes = "Patient requests post-stroke recovery therapy session.",
                StartTime = start.AddDays(1),
                EndTime = start.AddDays(1).AddHours(1)
            };
        }

        public Guid UserId { get; }
        public Guid DoctorProfileId { get; }
        public Guid SpecialtyId { get; }
        public Guid PatientProfileId { get; }
        public Guid AppointmentId { get; }
        public Guid RequestedAppointmentId { get; }
        public string? AvatarUrl => profile.AvatarUrl;

        public Task<DoctorProfileRecord?> GetProfileByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(userId == UserId ? profile : null);
        }

        public Task<DoctorProfileRecord?> UpdateOwnProfileAsync(
            Guid userId,
            UpdateDoctorProfileCommand command,
            CancellationToken cancellationToken = default)
        {
            if (userId != UserId)
            {
                return Task.FromResult<DoctorProfileRecord?>(null);
            }

            profile = profile with
            {
                PhoneNumber = command.PhoneNumber,
                Bio = command.Bio,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            return Task.FromResult<DoctorProfileRecord?>(profile);
        }

        public Task<IReadOnlyList<DoctorAppointmentRecord>> GetAppointmentsByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DoctorAppointmentRecord>>(
                userId == UserId ? [appointment, requestedAppointment] : []);
        }

        public Task<DoctorAppointmentRecord?> GetAppointmentByUserIdAsync(
            Guid userId,
            Guid appointmentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DoctorAppointmentRecord?>(
                userId == UserId
                    ? appointmentId == AppointmentId
                        ? appointment
                        : appointmentId == RequestedAppointmentId
                            ? requestedAppointment
                            : null
                    : null);
        }

        public Task<DoctorDashboardSnapshot?> GetDashboardSnapshotAsync(
            Guid userId,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            if (userId != UserId)
            {
                return Task.FromResult<DoctorDashboardSnapshot?>(null);
            }

            return Task.FromResult<DoctorDashboardSnapshot?>(new DoctorDashboardSnapshot(
                profile,
                2,
                1,
                3,
                4,
                appointment));
        }

        public Task<string?> UpdateAvatarAsync(
            Guid userId,
            string avatarUrl,
            CancellationToken cancellationToken = default)
        {
            if (userId != UserId)
            {
                return Task.FromResult<string?>(null);
            }

            profile = profile with { AvatarUrl = avatarUrl };

            return Task.FromResult<string?>(avatarUrl);
        }

        public Task<DoctorProfileRecord?> SubmitPublicProfileForReviewAsync(
            Guid userId,
            DateTimeOffset submittedAt,
            CancellationToken cancellationToken = default)
        {
            if (userId != UserId)
            {
                return Task.FromResult<DoctorProfileRecord?>(null);
            }

            profile = profile with
            {
                PublicProfileReviewStatus = DoctorProfileReviewStatus.Submitted,
                PublicProfileApproved = false,
                SubmittedForReviewAt = submittedAt,
                UpdatedAt = submittedAt
            };

            return Task.FromResult<DoctorProfileRecord?>(profile);
        }

        public Task<IReadOnlyList<DoctorAppointmentRecord>> GetRequestedAppointmentsByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DoctorAppointmentRecord>>(
                userId == UserId ? [requestedAppointment] : []);
        }

        public Task<DoctorAppointmentRecord?> AcceptAppointmentRequestAsync(
            Guid userId,
            Guid appointmentId,
            CancellationToken cancellationToken = default)
        {
            if (userId != UserId || appointmentId != RequestedAppointmentId)
            {
                return Task.FromResult<DoctorAppointmentRecord?>(null);
            }

            requestedAppointment = requestedAppointment with
            {
                Status = AppointmentStatus.PendingPayment
            };

            return Task.FromResult<DoctorAppointmentRecord?>(requestedAppointment);
        }

        public Task<DoctorAppointmentRecord?> RejectAppointmentRequestAsync(
            Guid userId,
            Guid appointmentId,
            string rejectionReason,
            CancellationToken cancellationToken = default)
        {
            if (userId != UserId || appointmentId != RequestedAppointmentId)
            {
                return Task.FromResult<DoctorAppointmentRecord?>(null);
            }

            requestedAppointment = requestedAppointment with
            {
                Status = AppointmentStatus.Rejected
            };

            return Task.FromResult<DoctorAppointmentRecord?>(requestedAppointment);
        }
    }

    private sealed class FakeDoctorAvatarStorage : IDoctorAvatarStorage
    {
        public string? Extension { get; private set; }

        public Task<string> SaveAsync(
            Stream content,
            string fileExtension,
            CancellationToken cancellationToken = default)
        {
            Extension = fileExtension;
            return Task.FromResult($"/uploads/doctor-avatars/test{fileExtension}");
        }
    }
}
