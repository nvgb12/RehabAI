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
            CreateFailureReason = AppointmentFailureReason.SlotUnavailable
        };
        var service = new AppointmentBookingService(repository);

        var result = await service.CreateAsync(repository.ValidCommand());

        Assert.False(result.Succeeded);
        Assert.Equal(AppointmentFailureReason.SlotUnavailable, result.FailureReason);
    }

    [Fact]
    public async Task CreateAsync_WhenSlotIsDoubleBooked_ReturnsConflictReason()
    {
        var repository = new FakeAppointmentBookingRepository
        {
            CreateFailureReason = AppointmentFailureReason.DoubleBooked
        };
        var service = new AppointmentBookingService(repository);

        var result = await service.CreateAsync(repository.ValidCommand());

        Assert.False(result.Succeeded);
        Assert.Equal(AppointmentFailureReason.DoubleBooked, result.FailureReason);
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
        public AppointmentFailureReason? CreateFailureReason { get; set; }

        public CreateAppointmentCommand ValidCommand()
        {
            return new CreateAppointmentCommand(
                PatientProfileId,
                DoctorProfileId,
                MedicalServiceId,
                ScheduleSlotId,
                "Lower back pain consultation");
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
                draft.Reason);

            appointments.Add(appointment);

            return Task.FromResult(new CreateAppointmentRepositoryResult(true, appointment, null, null));
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
