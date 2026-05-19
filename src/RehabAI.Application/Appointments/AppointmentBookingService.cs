namespace RehabAI.Application.Appointments;

public sealed class AppointmentBookingService(IAppointmentBookingRepository repository) : IAppointmentBookingService
{
    private const int DefaultSoftReserveMinutes = 10;

    public async Task<AppointmentResult> CreateAsync(
        CreateAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationMessage = ValidateCommand(command);

        if (validationMessage is not null)
        {
            return Failed(validationMessage, AppointmentFailureReason.Validation);
        }

        var patient = await repository.GetPatientStateAsync(command.PatientProfileId, cancellationToken);

        if (patient is null)
        {
            return Failed("Patient profile was not found.", AppointmentFailureReason.PatientNotFound);
        }

        if (!patient.IsActive)
        {
            return Failed("Only active Patient accounts can book appointments.", AppointmentFailureReason.PatientNotActive);
        }

        if (!patient.HasPatientRole)
        {
            return Failed("User linked to the patient profile does not have the Patient role.", AppointmentFailureReason.PatientRoleMissing);
        }

        var doctor = await repository.GetDoctorStateAsync(command.DoctorProfileId, cancellationToken);

        if (doctor is null)
        {
            return Failed("Doctor profile was not found.", AppointmentFailureReason.DoctorNotFound);
        }

        if (!doctor.IsPublicBookable)
        {
            return Failed("Doctor is not publicly bookable.", AppointmentFailureReason.DoctorNotPublicBookable);
        }

        if (!await repository.MedicalServiceIsActiveAsync(command.MedicalServiceId, cancellationToken))
        {
            return Failed("Medical service was not found or is not active.", AppointmentFailureReason.MedicalServiceNotFound);
        }

        var softReserveMinutes = await repository.GetSoftReserveMinutesAsync(cancellationToken) ?? DefaultSoftReserveMinutes;
        if (softReserveMinutes <= 0)
        {
            softReserveMinutes = DefaultSoftReserveMinutes;
        }

        var draft = new CreateAppointmentDraft(
            command.PatientProfileId,
            patient.UserId,
            command.DoctorProfileId,
            command.MedicalServiceId,
            command.ScheduleSlotId,
            NormalizeOptional(command.Reason),
            DateTimeOffset.UtcNow.AddMinutes(softReserveMinutes));

        var created = await repository.CreatePendingPaymentAppointmentAsync(draft, cancellationToken);

        if (!created.Succeeded)
        {
            return Failed(
                created.Message ?? "Appointment could not be created.",
                created.FailureReason ?? AppointmentFailureReason.Validation);
        }

        return new AppointmentResult(
            true,
            "Appointment created successfully.",
            ToResponse(created.Appointment!));
    }

    public async Task<AppointmentResponse?> GetByIdAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        if (appointmentId == Guid.Empty)
        {
            return null;
        }

        var appointment = await repository.GetByIdAsync(appointmentId, cancellationToken);

        return appointment is null ? null : ToResponse(appointment);
    }

    public async Task<AppointmentResult> ConfirmPaymentAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        if (appointmentId == Guid.Empty)
        {
            return Failed("Appointment id is required.", AppointmentFailureReason.Validation);
        }

        var confirmed = await repository.ConfirmPaymentPlaceholderAsync(appointmentId, cancellationToken);

        if (!confirmed.Succeeded)
        {
            return Failed(
                confirmed.Message ?? "Appointment payment could not be confirmed.",
                confirmed.FailureReason ?? AppointmentFailureReason.Validation);
        }

        return new AppointmentResult(
            true,
            "Payment confirmed. Appointment is now confirmed.",
            ToResponse(confirmed.Appointment!));
    }

    public async Task<AppointmentResult> CancelAsync(
        Guid appointmentId,
        string? cancellationReason,
        CancellationToken cancellationToken = default)
    {
        if (appointmentId == Guid.Empty)
        {
            return Failed("Appointment id is required.", AppointmentFailureReason.Validation);
        }

        var normalizedReason = NormalizeOptional(cancellationReason);
        if (normalizedReason is null)
        {
            return Failed("Cancellation reason is required.", AppointmentFailureReason.Validation);
        }

        var cancelled = await repository.CancelAppointmentAsync(
            appointmentId,
            normalizedReason,
            cancellationToken);

        if (!cancelled.Succeeded)
        {
            return Failed(
                cancelled.Message ?? "Appointment could not be cancelled.",
                cancelled.FailureReason ?? AppointmentFailureReason.Validation);
        }

        return new AppointmentResult(
            true,
            "Appointment cancelled successfully.",
            ToResponse(cancelled.Appointment!));
    }

    public async Task<IReadOnlyList<AppointmentResponse>> GetPatientAppointmentsAsync(
        Guid patientProfileId,
        CancellationToken cancellationToken = default)
    {
        if (patientProfileId == Guid.Empty)
        {
            return [];
        }

        var appointments = await repository.GetByPatientProfileIdAsync(patientProfileId, cancellationToken);

        return appointments.Select(ToResponse).ToList();
    }

    private static string? ValidateCommand(CreateAppointmentCommand command)
    {
        if (command.PatientProfileId == Guid.Empty)
        {
            return "Patient profile is required.";
        }

        if (command.DoctorProfileId == Guid.Empty)
        {
            return "Doctor profile is required.";
        }

        if (command.MedicalServiceId == Guid.Empty)
        {
            return "Medical service is required.";
        }

        if (command.ScheduleSlotId == Guid.Empty)
        {
            return "Schedule slot is required.";
        }

        return null;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static AppointmentResult Failed(string message, AppointmentFailureReason reason)
    {
        return new AppointmentResult(false, message, FailureReason: reason);
    }

    private static AppointmentResponse ToResponse(AppointmentRecord record)
    {
        return new AppointmentResponse(
            record.Id,
            record.PatientProfileId,
            record.DoctorProfileId,
            record.MedicalServiceId,
            record.ScheduleSlotId,
            record.Status.ToString(),
            record.StartTime,
            record.EndTime,
            record.ReservedUntil,
            record.Reason,
            record.CancellationReason);
    }
}
