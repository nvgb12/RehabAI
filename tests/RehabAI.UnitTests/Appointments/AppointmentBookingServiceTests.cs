using RehabAI.Application.Appointments;
using RehabAI.Domain.Enums;

namespace RehabAI.UnitTests.Appointments;

public class AppointmentBookingServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidCommand_CreatesPendingPaymentAppointment()
    {
        var repository = new FakeAppointmentBookingRepository();
        var service = new AppointmentBookingService(repository);

        var result = await service.CreateAsync(repository.ValidCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(nameof(AppointmentStatus.PendingPayment), result.Appointment!.Status);
        Assert.NotNull(result.Appointment.ReservedUntil);
        Assert.Equal(repository.PatientProfileId, result.Appointment.PatientProfileId);
    }

    [Fact]
    public async Task CreateAsync_WhenPatientIsNotActive_ReturnsForbiddenReason()
    {
        var repository = new FakeAppointmentBookingRepository
        {
            PatientIsActive = false
        };
        var service = new AppointmentBookingService(repository);

        var result = await service.CreateAsync(repository.ValidCommand());

        Assert.False(result.Succeeded);
        Assert.Equal(AppointmentFailureReason.PatientNotActive, result.FailureReason);
    }

    [Fact]
    public async Task CreateAsync_WhenDoctorIsNotPublicBookable_ReturnsFailure()
    {
        var repository = new FakeAppointmentBookingRepository
        {
            DoctorIsPublicBookable = false
        };
        var service = new AppointmentBookingService(repository);

        var result = await service.CreateAsync(repository.ValidCommand());

        Assert.False(result.Succeeded);
        Assert.Equal(AppointmentFailureReason.DoctorNotPublicBookable, result.FailureReason);
    }

    [Fact]
    public async Task CreateAsync_WhenSlotIsUnavailable_ReturnsConflictReason()
    {
        var repository = new FakeAppointmentBookingRepository
        {
            SlotStatus = ScheduleSlotStatus.Disabled
        };
        var service = new AppointmentBookingService(repository);

        var result = await service.CreateAsync(repository.ValidCommand());

        Assert.False(result.Succeeded);
        Assert.Equal(AppointmentFailureReason.SlotUnavailable, result.FailureReason);
    }

    [Fact]
    public async Task CreateAsync_WhenScheduleSlotIsMissing_ReturnsValidationFailure()
    {
        var repository = new FakeAppointmentBookingRepository();
        var service = new AppointmentBookingService(repository);

        var command = repository.ValidCommand() with
        {
            ScheduleSlotId = Guid.Empty
        };

        var result = await service.CreateAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal(AppointmentFailureReason.Validation, result.FailureReason);
    }

    [Fact]
    public async Task CreateAsync_WhenSlotBelongsToAnotherDoctor_ReturnsSlotNotFound()
    {
        var repository = new FakeAppointmentBookingRepository
        {
            SlotDoctorProfileId = Guid.NewGuid()
        };
        var service = new AppointmentBookingService(repository);

        var result = await service.CreateAsync(repository.ValidCommand());

        Assert.False(result.Succeeded);
        Assert.Equal(AppointmentFailureReason.SlotNotFound, result.FailureReason);
    }

    [Fact]
    public async Task CreateAsync_WhenSlotIsBooked_ReturnsSlotUnavailable()
    {
        var repository = new FakeAppointmentBookingRepository
        {
            SlotStatus = ScheduleSlotStatus.Booked
        };
        var service = new AppointmentBookingService(repository);

        var result = await service.CreateAsync(repository.ValidCommand());

        Assert.False(result.Succeeded);
        Assert.Equal(AppointmentFailureReason.SlotUnavailable, result.FailureReason);
    }

    [Fact]
    public async Task CreateAsync_WhenSlotIsInThePast_ReturnsSlotUnavailable()
    {
        var repository = new FakeAppointmentBookingRepository
        {
            SlotStartTime = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        var service = new AppointmentBookingService(repository);

        var result = await service.CreateAsync(repository.ValidCommand());

        Assert.False(result.Succeeded);
        Assert.Equal(AppointmentFailureReason.SlotUnavailable, result.FailureReason);
    }

    [Fact]
    public async Task CreateAsync_WhenSlotHasActiveAppointment_ReturnsDoubleBooked()
    {
        var repository = new FakeAppointmentBookingRepository
        {
            SlotHasActiveAppointment = true
        };
        var service = new AppointmentBookingService(repository);

        var result = await service.CreateAsync(repository.ValidCommand());

        Assert.False(result.Succeeded);
        Assert.Equal(AppointmentFailureReason.DoubleBooked, result.FailureReason);
    }

    [Fact]
    public async Task CreateAsync_WhenSlotIsDoubleBooked_ReturnsConflictReason()
    {
        var repository = new FakeAppointmentBookingRepository
        {
            SlotHasActiveAppointment = true
        };
        var service = new AppointmentBookingService(repository);

        var result = await service.CreateAsync(repository.ValidCommand());

        Assert.False(result.Succeeded);
        Assert.Equal(AppointmentFailureReason.DoubleBooked, result.FailureReason);
    }

    [Fact]
    public async Task CreateRequestAsync_WithApprovedDoctorWithoutSlots_CreatesRequestedAppointment()
    {
        var repository = new FakeAppointmentBookingRepository();
        var service = new AppointmentBookingService(repository);

        var result = await service.CreateRequestAsync(repository.ValidRequestCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(nameof(AppointmentStatus.Requested), result.Appointment!.Status);
        Assert.Null(result.Appointment.ScheduleSlotId);
        Assert.Equal("Post-stroke rehabilitation consultation", result.Appointment.Reason);
    }

    [Fact]
    public async Task CreateRequestAsync_WithApprovedDoctorEvenIfSlotsExist_CreatesRequestedAppointment()
    {
        var repository = new FakeAppointmentBookingRepository
        {
            SlotStatus = ScheduleSlotStatus.Available
        };
        var service = new AppointmentBookingService(repository);

        var result = await service.CreateRequestAsync(repository.ValidRequestCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(nameof(AppointmentStatus.Requested), result.Appointment!.Status);
    }

    [Fact]
    public async Task CreateRequestAsync_WhenDoctorIsNotPublicBookable_ReturnsFailure()
    {
        var repository = new FakeAppointmentBookingRepository
        {
            DoctorIsPublicBookable = false
        };
        var service = new AppointmentBookingService(repository);

        var result = await service.CreateRequestAsync(repository.ValidRequestCommand());

        Assert.False(result.Succeeded);
        Assert.Equal(AppointmentFailureReason.DoctorNotPublicBookable, result.FailureReason);
    }

    [Fact]
    public async Task CreateRequestAsync_WhenMedicalServiceIsInactive_ReturnsFailure()
    {
        var repository = new FakeAppointmentBookingRepository
        {
            MedicalServiceIsActive = false
        };
        var service = new AppointmentBookingService(repository);

        var result = await service.CreateRequestAsync(repository.ValidRequestCommand());

        Assert.False(result.Succeeded);
        Assert.Equal(AppointmentFailureReason.MedicalServiceNotFound, result.FailureReason);
    }

    [Fact]
    public async Task CreateRequestAsync_WhenPreferredTimeIsInvalid_ReturnsValidationFailure()
    {
        var repository = new FakeAppointmentBookingRepository();
        var service = new AppointmentBookingService(repository);
        var validCommand = repository.ValidRequestCommand();
        var command = validCommand with
        {
            PreferredEndTime = validCommand.PreferredStartTime
        };

        var result = await service.CreateRequestAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal(AppointmentFailureReason.Validation, result.FailureReason);
    }

    [Fact]
    public async Task GetPatientAppointmentsAsync_ReturnsPatientAppointments()
    {
        var repository = new FakeAppointmentBookingRepository();
        var service = new AppointmentBookingService(repository);
        var create = await service.CreateAsync(repository.ValidCommand());

        var appointments = await service.GetPatientAppointmentsAsync(repository.PatientProfileId);

        Assert.Single(appointments);
        Assert.Equal(create.Appointment!.Id, appointments[0].Id);
    }

    [Fact]
    public async Task ConfirmPaymentAsync_WhenAppointmentIsPendingPayment_ConfirmsAppointment()
    {
        var repository = new FakeAppointmentBookingRepository();
        var service = new AppointmentBookingService(repository);
        var create = await service.CreateAsync(repository.ValidCommand());

        var result = await service.ConfirmPaymentAsync(create.Appointment!.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(nameof(AppointmentStatus.Confirmed), result.Appointment!.Status);
        Assert.Null(result.Appointment.ReservedUntil);
        Assert.True(repository.SlotWasBooked);
        Assert.True(repository.ReservationWasCleared);
    }

    [Fact]
    public async Task ConfirmPaymentAsync_WhenAppointmentDoesNotExist_ReturnsNotFoundReason()
    {
        var repository = new FakeAppointmentBookingRepository();
        var service = new AppointmentBookingService(repository);

        var result = await service.ConfirmPaymentAsync(Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(AppointmentFailureReason.AppointmentNotFound, result.FailureReason);
    }

    [Fact]
    public async Task ConfirmPaymentAsync_WhenAppointmentIsNotPendingPayment_ReturnsConflictReason()
    {
        var repository = new FakeAppointmentBookingRepository();
        var service = new AppointmentBookingService(repository);
        var create = await service.CreateAsync(repository.ValidCommand());
        await service.ConfirmPaymentAsync(create.Appointment!.Id);

        var result = await service.ConfirmPaymentAsync(create.Appointment.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(AppointmentFailureReason.AppointmentNotPendingPayment, result.FailureReason);
    }

    [Fact]
    public async Task CancelAsync_WhenAppointmentIsPendingPayment_CancelsAppointment()
    {
        var repository = new FakeAppointmentBookingRepository();
        var service = new AppointmentBookingService(repository);
        var create = await service.CreateAsync(repository.ValidCommand());

        var result = await service.CancelAsync(
            create.Appointment!.Id,
            "Patient needs to reschedule stroke mobility assessment.");

        Assert.True(result.Succeeded);
        Assert.Equal(nameof(AppointmentStatus.Cancelled), result.Appointment!.Status);
        Assert.Equal("Patient needs to reschedule stroke mobility assessment.", result.Appointment.CancellationReason);
        Assert.Null(result.Appointment.ReservedUntil);
        Assert.True(repository.SlotWasReleased);
        Assert.True(repository.ReservationWasCleared);
    }

    [Fact]
    public async Task CancelAsync_WhenAppointmentIsConfirmed_CancelsAppointment()
    {
        var repository = new FakeAppointmentBookingRepository();
        var service = new AppointmentBookingService(repository);
        var create = await service.CreateAsync(repository.ValidCommand());
        await service.ConfirmPaymentAsync(create.Appointment!.Id);

        var result = await service.CancelAsync(
            create.Appointment.Id,
            "Patient cannot attend neurological rehabilitation follow-up.");

        Assert.True(result.Succeeded);
        Assert.Equal(nameof(AppointmentStatus.Cancelled), result.Appointment!.Status);
        Assert.True(repository.SlotWasReleased);
    }

    [Fact]
    public async Task CancelAsync_WhenAppointmentDoesNotExist_ReturnsNotFoundReason()
    {
        var repository = new FakeAppointmentBookingRepository();
        var service = new AppointmentBookingService(repository);

        var result = await service.CancelAsync(
            Guid.NewGuid(),
            "Patient needs to reschedule.");

        Assert.False(result.Succeeded);
        Assert.Equal(AppointmentFailureReason.AppointmentNotFound, result.FailureReason);
    }

    [Fact]
    public async Task CancelAsync_WhenAppointmentIsAlreadyCancelled_ReturnsConflictReason()
    {
        var repository = new FakeAppointmentBookingRepository();
        var service = new AppointmentBookingService(repository);
        var create = await service.CreateAsync(repository.ValidCommand());
        await service.CancelAsync(create.Appointment!.Id, "Patient requested cancellation.");

        var result = await service.CancelAsync(
            create.Appointment.Id,
            "Duplicate cancellation request.");

        Assert.False(result.Succeeded);
        Assert.Equal(AppointmentFailureReason.AppointmentAlreadyCancelled, result.FailureReason);
    }

    private sealed class FakeAppointmentBookingRepository : IAppointmentBookingRepository
    {
        private readonly List<AppointmentRecord> appointments = [];

        public Guid PatientProfileId { get; } = Guid.NewGuid();
        public Guid PatientUserId { get; } = Guid.NewGuid();
        public Guid DoctorProfileId { get; } = Guid.NewGuid();
        public Guid MedicalServiceId { get; } = Guid.NewGuid();
        public Guid ScheduleSlotId { get; } = Guid.NewGuid();

        public bool PatientIsActive { get; set; } = true;
        public bool PatientHasRole { get; set; } = true;
        public bool DoctorIsPublicBookable { get; set; } = true;
        public bool MedicalServiceIsActive { get; set; } = true;
        public Guid? SlotDoctorProfileId { get; set; }
        public ScheduleSlotStatus SlotStatus { get; set; } = ScheduleSlotStatus.Available;
        public DateTimeOffset SlotStartTime { get; set; } = DateTimeOffset.UtcNow.AddDays(1);
        public bool SlotHasActiveAppointment { get; set; }
        public AppointmentFailureReason? CreateFailureReason { get; set; }
        public bool SlotWasBooked { get; private set; }
        public bool SlotWasReleased { get; private set; }
        public bool ReservationWasCleared { get; private set; }

        public CreateAppointmentCommand ValidCommand()
        {
            return new CreateAppointmentCommand(
                PatientProfileId,
                DoctorProfileId,
                MedicalServiceId,
                ScheduleSlotId,
                "Post-stroke rehabilitation consultation");
        }

        public CreateAppointmentRequestCommand ValidRequestCommand()
        {
            var preferredStart = DateTimeOffset.UtcNow.AddDays(2);

            return new CreateAppointmentRequestCommand(
                PatientProfileId,
                DoctorProfileId,
                MedicalServiceId,
                preferredStart,
                preferredStart.AddHours(1),
                "Post-stroke rehabilitation consultation");
        }

        public Task<PatientBookingState?> GetPatientStateAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken = default)
        {
            if (patientProfileId != PatientProfileId)
            {
                return Task.FromResult<PatientBookingState?>(null);
            }

            return Task.FromResult<PatientBookingState?>(new PatientBookingState(
                PatientProfileId,
                PatientUserId,
                PatientIsActive,
                PatientHasRole));
        }

        public Task<DoctorBookingState?> GetDoctorStateAsync(
            Guid doctorProfileId,
            CancellationToken cancellationToken = default)
        {
            if (doctorProfileId != DoctorProfileId)
            {
                return Task.FromResult<DoctorBookingState?>(null);
            }

            return Task.FromResult<DoctorBookingState?>(new DoctorBookingState(
                DoctorProfileId,
                Guid.NewGuid(),
                DoctorIsPublicBookable));
        }

        public Task<bool> MedicalServiceIsActiveAsync(Guid medicalServiceId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(medicalServiceId == MedicalServiceId && MedicalServiceIsActive);
        }

        public Task<ScheduleSlotBookingState?> GetScheduleSlotStateAsync(
            Guid scheduleSlotId,
            CancellationToken cancellationToken = default)
        {
            if (scheduleSlotId != ScheduleSlotId)
            {
                return Task.FromResult<ScheduleSlotBookingState?>(null);
            }

            return Task.FromResult<ScheduleSlotBookingState?>(new ScheduleSlotBookingState(
                ScheduleSlotId,
                SlotDoctorProfileId ?? DoctorProfileId,
                SlotStatus,
                SlotStartTime,
                SlotHasActiveAppointment));
        }

        public Task<int?> GetSoftReserveMinutesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<int?>(10);
        }

        public Task<CreateAppointmentRepositoryResult> CreatePendingPaymentAppointmentAsync(
            CreateAppointmentDraft draft,
            CancellationToken cancellationToken = default)
        {
            if (CreateFailureReason.HasValue)
            {
                return Task.FromResult(new CreateAppointmentRepositoryResult(
                    false,
                    null,
                    CreateFailureReason,
                    "Appointment could not be created."));
            }

            var start = DateTimeOffset.UtcNow.AddDays(1);
            var appointment = new AppointmentRecord(
                Guid.NewGuid(),
                draft.PatientProfileId,
                draft.PatientUserId,
                draft.DoctorProfileId,
                draft.MedicalServiceId,
                draft.ScheduleSlotId,
                AppointmentStatus.PendingPayment,
                start,
                start.AddHours(1),
                draft.ReservedUntil,
                draft.Reason,
                null);

            appointments.Add(appointment);

            return Task.FromResult(new CreateAppointmentRepositoryResult(true, appointment, null, null));
        }

        public Task<CreateAppointmentRepositoryResult> CreateRequestedAppointmentAsync(
            CreateAppointmentRequestDraft draft,
            CancellationToken cancellationToken = default)
        {
            var appointment = new AppointmentRecord(
                Guid.NewGuid(),
                draft.PatientProfileId,
                draft.PatientUserId,
                draft.DoctorProfileId,
                draft.MedicalServiceId,
                null,
                AppointmentStatus.Requested,
                draft.PreferredStartTime,
                draft.PreferredEndTime,
                null,
                draft.Reason,
                null);

            appointments.Add(appointment);

            return Task.FromResult(new CreateAppointmentRepositoryResult(true, appointment, null, null));
        }

        public Task<CreateAppointmentRepositoryResult> ConfirmPaymentPlaceholderAsync(
            Guid appointmentId,
            CancellationToken cancellationToken = default)
        {
            var index = appointments.FindIndex(appointment => appointment.Id == appointmentId);

            if (index < 0)
            {
                return Task.FromResult(new CreateAppointmentRepositoryResult(
                    false,
                    null,
                    AppointmentFailureReason.AppointmentNotFound,
                    "Appointment was not found."));
            }

            var appointment = appointments[index];

            if (appointment.Status != AppointmentStatus.PendingPayment)
            {
                return Task.FromResult(new CreateAppointmentRepositoryResult(
                    false,
                    null,
                    AppointmentFailureReason.AppointmentNotPendingPayment,
                    "Only appointments with PendingPayment status can be confirmed."));
            }

            var confirmed = appointment with
            {
                Status = AppointmentStatus.Confirmed,
                ReservedUntil = null
            };

            appointments[index] = confirmed;
            SlotWasBooked = true;
            ReservationWasCleared = true;

            return Task.FromResult(new CreateAppointmentRepositoryResult(true, confirmed, null, null));
        }

        public Task<CreateAppointmentRepositoryResult> CancelAppointmentAsync(
            Guid appointmentId,
            string cancellationReason,
            CancellationToken cancellationToken = default)
        {
            var index = appointments.FindIndex(appointment => appointment.Id == appointmentId);

            if (index < 0)
            {
                return Task.FromResult(new CreateAppointmentRepositoryResult(
                    false,
                    null,
                    AppointmentFailureReason.AppointmentNotFound,
                    "Appointment was not found."));
            }

            var appointment = appointments[index];

            if (appointment.Status == AppointmentStatus.Cancelled)
            {
                return Task.FromResult(new CreateAppointmentRepositoryResult(
                    false,
                    null,
                    AppointmentFailureReason.AppointmentAlreadyCancelled,
                    "Appointment is already cancelled."));
            }

            if (appointment.Status is not (AppointmentStatus.PendingPayment or AppointmentStatus.Confirmed))
            {
                return Task.FromResult(new CreateAppointmentRepositoryResult(
                    false,
                    null,
                    AppointmentFailureReason.AppointmentNotCancellable,
                    "Only PendingPayment or Confirmed appointments can be cancelled."));
            }

            var cancelled = appointment with
            {
                Status = AppointmentStatus.Cancelled,
                ReservedUntil = null,
                CancellationReason = cancellationReason
            };

            appointments[index] = cancelled;
            SlotWasReleased = true;
            ReservationWasCleared = true;

            return Task.FromResult(new CreateAppointmentRepositoryResult(true, cancelled, null, null));
        }

        public Task<AppointmentRecord?> GetByIdAsync(Guid appointmentId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(appointments.SingleOrDefault(appointment => appointment.Id == appointmentId));
        }

        public Task<IReadOnlyList<AppointmentRecord>> GetByPatientProfileIdAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AppointmentRecord>>(
                appointments.Where(appointment => appointment.PatientProfileId == patientProfileId).ToList());
        }
    }
}
