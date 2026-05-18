using System.Data;
using Microsoft.EntityFrameworkCore;
using RehabAI.Application.Appointments;
using RehabAI.Domain.Entities;
using RehabAI.Domain.Enums;
using RehabAI.Infrastructure.Database;

namespace RehabAI.Infrastructure.Appointments;

public sealed class EfAppointmentBookingRepository(AppDbContext dbContext) : IAppointmentBookingRepository
{
    private const string PatientRoleName = "Patient";
    private const string DoctorRoleName = "Doctor";
    private const string SoftReserveMinutesSettingKey = "Appointment.SoftReserveMinutes";

    public async Task<PatientBookingState?> GetPatientStateAsync(
        Guid patientProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PatientProfiles
            .Where(profile => profile.Id == patientProfileId && !profile.IsDeleted)
            .Select(profile => new PatientBookingState(
                profile.Id,
                profile.UserId,
                profile.User != null &&
                    !profile.User.IsDeleted &&
                    profile.User.Status == AccountStatus.Active,
                profile.User != null &&
                    profile.User.Roles.Any(userRole =>
                        userRole.Role != null &&
                        !userRole.Role.IsDeleted &&
                        userRole.Role.Name == PatientRoleName)))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<DoctorBookingState?> GetDoctorStateAsync(
        Guid doctorProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.DoctorProfiles
            .Where(profile => profile.Id == doctorProfileId && !profile.IsDeleted)
            .Select(profile => new DoctorBookingState(
                profile.Id,
                profile.UserId,
                profile.PublicProfileApproved &&
                    profile.User != null &&
                    !profile.User.IsDeleted &&
                    profile.User.Status == AccountStatus.Active &&
                    profile.User.Roles.Any(userRole =>
                        userRole.Role != null &&
                        !userRole.Role.IsDeleted &&
                        userRole.Role.Name == DoctorRoleName)))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> MedicalServiceIsActiveAsync(Guid medicalServiceId, CancellationToken cancellationToken = default)
    {
        return dbContext.MedicalServices.AnyAsync(
            service => service.Id == medicalServiceId && service.IsActive && !service.IsDeleted,
            cancellationToken);
    }

    public async Task<int?> GetSoftReserveMinutesAsync(CancellationToken cancellationToken = default)
    {
        var value = await dbContext.SystemSettings
            .Where(setting => setting.SettingKey == SoftReserveMinutesSettingKey)
            .Select(setting => setting.SettingValue)
            .SingleOrDefaultAsync(cancellationToken);

        return int.TryParse(value, out var minutes) ? minutes : null;
    }

    public async Task<CreateAppointmentRepositoryResult> CreatePendingPaymentAppointmentAsync(
        CreateAppointmentDraft draft,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var now = DateTimeOffset.UtcNow;

        var slot = await dbContext.DoctorScheduleSlots
            .SingleOrDefaultAsync(
                slot =>
                    slot.Id == draft.ScheduleSlotId &&
                    slot.DoctorProfileId == draft.DoctorProfileId &&
                    !slot.IsDeleted,
                cancellationToken);

        if (slot is null)
        {
            return Failed("Schedule slot was not found.", AppointmentFailureReason.SlotNotFound);
        }

        if (slot.StartTime <= now || slot.Status != ScheduleSlotStatus.Available)
        {
            return Failed("Schedule slot is not available for booking.", AppointmentFailureReason.SlotUnavailable);
        }

        var hasActiveAppointment = await dbContext.Appointments.AnyAsync(
            appointment =>
                appointment.DoctorScheduleSlotId == draft.ScheduleSlotId &&
                !appointment.IsDeleted &&
                (appointment.Status == AppointmentStatus.PendingPayment ||
                    appointment.Status == AppointmentStatus.Pending ||
                    appointment.Status == AppointmentStatus.Confirmed),
            cancellationToken);

        if (hasActiveAppointment)
        {
            return Failed("Schedule slot has already been booked.", AppointmentFailureReason.DoubleBooked);
        }

        slot.Status = ScheduleSlotStatus.SoftReserved;
        slot.ReservedUntil = draft.ReservedUntil;
        slot.UpdatedAt = now;

        var appointment = new Appointment
        {
            PatientId = draft.PatientUserId,
            DoctorProfileId = draft.DoctorProfileId,
            MedicalServiceId = draft.MedicalServiceId,
            DoctorScheduleSlotId = draft.ScheduleSlotId,
            StartTime = slot.StartTime,
            EndTime = slot.EndTime,
            Status = AppointmentStatus.PendingPayment,
            Notes = draft.Reason,
            SoftReservedUntil = draft.ReservedUntil,
            CreatedAt = now
        };

        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CreateAppointmentRepositoryResult(
            true,
            ToRecord(appointment, draft.PatientProfileId),
            null,
            null);
    }

    public async Task<AppointmentRecord?> GetByIdAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Where(appointment => !appointment.IsDeleted && appointment.Id == appointmentId)
            .Join(
                dbContext.PatientProfiles.Where(profile => !profile.IsDeleted),
                appointment => appointment.PatientId,
                profile => profile.UserId,
                (appointment, profile) => new AppointmentRecord(
                    appointment.Id,
                    profile.Id,
                    appointment.PatientId,
                    appointment.DoctorProfileId,
                    appointment.MedicalServiceId,
                    appointment.DoctorScheduleSlotId,
                    appointment.Status,
                    appointment.StartTime,
                    appointment.EndTime,
                    appointment.SoftReservedUntil,
                    appointment.Notes))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AppointmentRecord>> GetByPatientProfileIdAsync(
        Guid patientProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Where(appointment => !appointment.IsDeleted)
            .Join(
                dbContext.PatientProfiles.Where(profile => !profile.IsDeleted && profile.Id == patientProfileId),
                appointment => appointment.PatientId,
                profile => profile.UserId,
                (appointment, profile) => new
                {
                    Appointment = appointment,
                    PatientProfile = profile
                })
            .OrderByDescending(row => row.Appointment.StartTime)
            .Select(row => new AppointmentRecord(
                row.Appointment.Id,
                row.PatientProfile.Id,
                row.Appointment.PatientId,
                row.Appointment.DoctorProfileId,
                row.Appointment.MedicalServiceId,
                row.Appointment.DoctorScheduleSlotId,
                row.Appointment.Status,
                row.Appointment.StartTime,
                row.Appointment.EndTime,
                row.Appointment.SoftReservedUntil,
                row.Appointment.Notes))
            .ToListAsync(cancellationToken);
    }

    private static AppointmentRecord ToRecord(Appointment appointment, Guid patientProfileId)
    {
        return new AppointmentRecord(
            appointment.Id,
            patientProfileId,
            appointment.PatientId,
            appointment.DoctorProfileId,
            appointment.MedicalServiceId,
            appointment.DoctorScheduleSlotId,
            appointment.Status,
            appointment.StartTime,
            appointment.EndTime,
            appointment.SoftReservedUntil,
            appointment.Notes);
    }

    private static CreateAppointmentRepositoryResult Failed(string message, AppointmentFailureReason reason)
    {
        return new CreateAppointmentRepositoryResult(false, null, reason, message);
    }

}
