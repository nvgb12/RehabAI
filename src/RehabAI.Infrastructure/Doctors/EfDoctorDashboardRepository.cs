using Microsoft.EntityFrameworkCore;
using RehabAI.Application.Doctors;
using RehabAI.Domain.Enums;
using RehabAI.Infrastructure.Database;

namespace RehabAI.Infrastructure.Doctors;

public sealed class EfDoctorDashboardRepository(AppDbContext dbContext) : IDoctorDashboardRepository
{
    public async Task<DoctorProfileRecord?> GetProfileByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await BuildProfileQuery(userId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<DoctorProfileRecord?> UpdateOwnProfileAsync(
        Guid userId,
        UpdateDoctorProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.DoctorProfiles
            .Include(doctorProfile => doctorProfile.User)
            .SingleOrDefaultAsync(
                doctorProfile =>
                    doctorProfile.UserId == userId &&
                    !doctorProfile.IsDeleted &&
                    doctorProfile.User != null &&
                    !doctorProfile.User.IsDeleted,
                cancellationToken);

        if (profile is null || profile.User is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        profile.User.PhoneNumber = command.PhoneNumber;
        profile.User.UpdatedAt = now;
        profile.Bio = command.Bio;
        profile.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetProfileByUserIdAsync(userId, cancellationToken);
    }

    public async Task<IReadOnlyList<DoctorAppointmentRecord>> GetAppointmentsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var doctorProfileId = await GetDoctorProfileIdByUserIdAsync(userId, cancellationToken);
        if (doctorProfileId is null)
        {
            return [];
        }

        var appointments = await GetAppointmentRecordsForDoctorAsync(
            doctorProfileId.Value,
            appointmentId: null,
            cancellationToken);

        return appointments
            .OrderBy(appointment => appointment.StartTime)
            .ThenBy(appointment => appointment.PatientName)
            .ToList();
    }

    public async Task<DoctorAppointmentRecord?> GetAppointmentByUserIdAsync(
        Guid userId,
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var doctorProfileId = await GetDoctorProfileIdByUserIdAsync(userId, cancellationToken);
        if (doctorProfileId is null)
        {
            return null;
        }

        var appointments = await GetAppointmentRecordsForDoctorAsync(
            doctorProfileId.Value,
            appointmentId,
            cancellationToken);

        return appointments.SingleOrDefault();
    }

    public async Task<DoctorDashboardSnapshot?> GetDashboardSnapshotAsync(
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var profile = await GetProfileByUserIdAsync(userId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        var futureAppointments = dbContext.Appointments
            .Where(appointment =>
                !appointment.IsDeleted &&
                appointment.DoctorProfileId == profile.DoctorProfileId &&
                appointment.StartTime >= now &&
                appointment.Status != AppointmentStatus.Cancelled &&
                appointment.Status != AppointmentStatus.Expired &&
                appointment.Status != AppointmentStatus.NoShow);

        var todayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);
        var todayEnd = todayStart.AddDays(1);

        var upcomingAppointmentCount = await futureAppointments.CountAsync(cancellationToken);
        var todayAppointmentCount = await dbContext.Appointments.CountAsync(
            appointment =>
                !appointment.IsDeleted &&
                appointment.DoctorProfileId == profile.DoctorProfileId &&
                appointment.StartTime >= todayStart &&
                appointment.StartTime < todayEnd &&
                appointment.Status != AppointmentStatus.Cancelled &&
                appointment.Status != AppointmentStatus.Expired,
            cancellationToken);
        var availableSlotCount = await dbContext.DoctorScheduleSlots.CountAsync(
            slot =>
                !slot.IsDeleted &&
                slot.DoctorProfileId == profile.DoctorProfileId &&
                slot.StartTime >= now &&
                slot.Status == ScheduleSlotStatus.Available,
            cancellationToken);
        var bookedSlotCount = await dbContext.DoctorScheduleSlots.CountAsync(
            slot =>
                !slot.IsDeleted &&
                slot.DoctorProfileId == profile.DoctorProfileId &&
                slot.StartTime >= now &&
                slot.Status == ScheduleSlotStatus.Booked,
            cancellationToken);

        var appointmentRecords = await GetAppointmentRecordsForDoctorAsync(
            profile.DoctorProfileId,
            appointmentId: null,
            cancellationToken);
        var nextAppointment = appointmentRecords
            .Where(appointment =>
                appointment.StartTime >= now &&
                appointment.Status != AppointmentStatus.Cancelled &&
                appointment.Status != AppointmentStatus.Expired &&
                appointment.Status != AppointmentStatus.NoShow)
            .OrderBy(appointment => appointment.StartTime)
            .FirstOrDefault();

        return new DoctorDashboardSnapshot(
            profile,
            upcomingAppointmentCount,
            todayAppointmentCount,
            availableSlotCount,
            bookedSlotCount,
            nextAppointment);
    }

    public async Task<string?> UpdateAvatarAsync(
        Guid userId,
        string avatarUrl,
        CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.DoctorProfiles
            .SingleOrDefaultAsync(
                doctorProfile =>
                    doctorProfile.UserId == userId &&
                    !doctorProfile.IsDeleted,
                cancellationToken);

        if (profile is null)
        {
            return null;
        }

        profile.AvatarUrl = avatarUrl;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return avatarUrl;
    }

    public async Task<IReadOnlyList<DoctorAppointmentRecord>> GetRequestedAppointmentsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var doctorProfileId = await GetDoctorProfileIdByUserIdAsync(userId, cancellationToken);
        if (doctorProfileId is null)
        {
            return [];
        }

        var appointments = await GetAppointmentRecordsForDoctorAsync(
            doctorProfileId.Value,
            appointmentId: null,
            cancellationToken);

        return appointments
            .Where(appointment => appointment.Status == AppointmentStatus.Requested)
            .OrderBy(appointment => appointment.StartTime)
            .ThenBy(appointment => appointment.PatientName)
            .ToList();
    }

    public async Task<DoctorAppointmentRecord?> AcceptAppointmentRequestAsync(
        Guid userId,
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        return await UpdateAppointmentRequestStatusAsync(
            userId,
            appointmentId,
            AppointmentStatus.PendingPayment,
            rejectionReason: null,
            cancellationToken);
    }

    public async Task<DoctorAppointmentRecord?> RejectAppointmentRequestAsync(
        Guid userId,
        Guid appointmentId,
        string rejectionReason,
        CancellationToken cancellationToken = default)
    {
        return await UpdateAppointmentRequestStatusAsync(
            userId,
            appointmentId,
            AppointmentStatus.Rejected,
            rejectionReason,
            cancellationToken);
    }

    public async Task<DoctorProfileRecord?> SubmitPublicProfileForReviewAsync(
        Guid userId,
        DateTimeOffset submittedAt,
        CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.DoctorProfiles
            .SingleOrDefaultAsync(
                doctorProfile =>
                    doctorProfile.UserId == userId &&
                    !doctorProfile.IsDeleted,
                cancellationToken);

        if (profile is null)
        {
            return null;
        }

        profile.PublicProfileReviewStatus = DoctorProfileReviewStatus.Submitted;
        profile.PublicProfileApproved = false;
        profile.SubmittedForReviewAt = submittedAt;
        profile.ReviewedAt = null;
        profile.ReviewedByAdminId = null;
        profile.PublicProfileRejectionReason = null;
        profile.UpdatedAt = submittedAt;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetProfileByUserIdAsync(userId, cancellationToken);
    }

    private async Task<DoctorAppointmentRecord?> UpdateAppointmentRequestStatusAsync(
        Guid userId,
        Guid appointmentId,
        AppointmentStatus targetStatus,
        string? rejectionReason,
        CancellationToken cancellationToken)
    {
        var doctorProfileId = await GetDoctorProfileIdByUserIdAsync(userId, cancellationToken);
        if (doctorProfileId is null)
        {
            return null;
        }

        var appointment = await dbContext.Appointments
            .SingleOrDefaultAsync(
                appointment =>
                    appointment.Id == appointmentId &&
                    !appointment.IsDeleted &&
                    appointment.DoctorProfileId == doctorProfileId.Value,
                cancellationToken);

        if (appointment is null)
        {
            return null;
        }

        if (appointment.Status != AppointmentStatus.Requested)
        {
            return await GetAppointmentByUserIdAsync(userId, appointmentId, cancellationToken);
        }

        appointment.Status = targetStatus;
        if (targetStatus == AppointmentStatus.Rejected)
        {
            appointment.CancellationReason = rejectionReason;
        }

        appointment.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetAppointmentByUserIdAsync(userId, appointmentId, cancellationToken);
    }

    private IQueryable<DoctorProfileRecord> BuildProfileQuery(Guid userId)
    {
        return dbContext.DoctorProfiles
            .AsNoTracking()
            .Where(profile =>
                profile.UserId == userId &&
                !profile.IsDeleted &&
                profile.User != null &&
                !profile.User.IsDeleted)
            .Select(profile => new DoctorProfileRecord(
                profile.Id,
                profile.UserId,
                profile.User!.FullName,
                profile.User.Email,
                profile.User.PhoneNumber,
                profile.User.Status,
                profile.User.EmailConfirmed,
                profile.SpecialtyId,
                profile.Specialty != null ? profile.Specialty.Name : string.Empty,
                profile.Bio,
                profile.PublicProfileApproved,
                profile.PublicProfileReviewStatus,
                profile.SubmittedForReviewAt,
                profile.ReviewedAt,
                profile.ReviewedByAdminId,
                profile.PublicProfileRejectionReason,
                profile.AvatarUrl,
                profile.CreatedAt,
                profile.UpdatedAt));
    }

    private async Task<Guid?> GetDoctorProfileIdByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.DoctorProfiles
            .AsNoTracking()
            .Where(profile =>
                profile.UserId == userId &&
                !profile.IsDeleted &&
                profile.User != null &&
                !profile.User.IsDeleted)
            .Select(profile => (Guid?)profile.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<DoctorAppointmentRecord>> GetAppointmentRecordsForDoctorAsync(
        Guid doctorProfileId,
        Guid? appointmentId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Appointments
            .AsNoTracking()
            .Where(appointment =>
                !appointment.IsDeleted &&
                appointment.DoctorProfileId == doctorProfileId);

        if (appointmentId.HasValue)
        {
            query = query.Where(appointment => appointment.Id == appointmentId.Value);
        }

        var appointmentRows = await query
            .Join(
                dbContext.PatientProfiles.AsNoTracking().Where(profile => !profile.IsDeleted),
                appointment => appointment.PatientId,
                patientProfile => patientProfile.UserId,
                (appointment, patientProfile) => new
                {
                    Appointment = appointment,
                    PatientProfile = patientProfile
                })
            .Join(
                dbContext.Users.AsNoTracking().Where(user => !user.IsDeleted),
                row => row.PatientProfile.UserId,
                user => user.Id,
                (row, user) => new
                {
                    row.Appointment,
                    row.PatientProfile,
                    PatientUser = user
                })
            .Join(
                dbContext.MedicalServices.AsNoTracking(),
                row => row.Appointment.MedicalServiceId,
                service => service.Id,
                (row, service) => new
                {
                    row.Appointment,
                    row.PatientProfile,
                    row.PatientUser,
                    Service = service
                })
            .Select(row => new DoctorAppointmentProjection(
                row.Appointment.Id,
                row.PatientProfile.Id,
                row.PatientUser.FullName,
                row.Appointment.MedicalServiceId,
                row.Service.Name,
                row.Appointment.DoctorScheduleSlotId,
                row.Appointment.StartTime,
                row.Appointment.EndTime,
                row.Appointment.Status,
                row.Appointment.Notes,
                row.Appointment.CreatedAt))
            .ToListAsync(cancellationToken);

        if (appointmentRows.Count == 0)
        {
            return [];
        }

        var appointmentIds = appointmentRows
            .Select(appointment => appointment.AppointmentId)
            .ToList();

        var paymentRows = await dbContext.Payments
            .AsNoTracking()
            .Where(payment =>
                !payment.IsDeleted &&
                payment.AppointmentId.HasValue &&
                appointmentIds.Contains(payment.AppointmentId.Value))
            .Select(payment => new
            {
                AppointmentId = payment.AppointmentId!.Value,
                payment.Status,
                payment.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var latestPaymentStatusByAppointmentId = paymentRows
            .GroupBy(payment => payment.AppointmentId)
            .ToDictionary(
                group => group.Key,
                group => (PaymentStatus?)group
                    .OrderByDescending(payment => payment.CreatedAt)
                    .First()
                    .Status);

        return appointmentRows
            .Select(row => new DoctorAppointmentRecord(
                row.AppointmentId,
                row.PatientProfileId,
                row.PatientName,
                row.MedicalServiceId,
                row.MedicalServiceName,
                row.DoctorScheduleSlotId,
                row.StartTime,
                row.EndTime,
                row.Status,
                latestPaymentStatusByAppointmentId.GetValueOrDefault(row.AppointmentId),
                row.Notes,
                row.CreatedAt))
            .ToList();
    }

    private sealed record DoctorAppointmentProjection(
        Guid AppointmentId,
        Guid PatientProfileId,
        string PatientName,
        Guid MedicalServiceId,
        string MedicalServiceName,
        Guid? DoctorScheduleSlotId,
        DateTimeOffset StartTime,
        DateTimeOffset EndTime,
        AppointmentStatus Status,
        string? Notes,
        DateTimeOffset CreatedAt);
}
