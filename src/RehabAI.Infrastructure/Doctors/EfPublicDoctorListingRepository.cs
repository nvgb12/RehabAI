using Microsoft.EntityFrameworkCore;
using RehabAI.Application.Doctors;
using RehabAI.Domain.Entities;
using RehabAI.Domain.Enums;
using RehabAI.Infrastructure.Database;

namespace RehabAI.Infrastructure.Doctors;

public sealed class EfPublicDoctorListingRepository(AppDbContext dbContext) : IPublicDoctorListingRepository
{
    private const string DoctorRoleName = "Doctor";

    public async Task<IReadOnlyList<PublicDoctorRecord>> SearchAsync(
        PublicDoctorSearchCriteria criteria,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var doctorRoleId = await GetDoctorRoleIdAsync(cancellationToken);

        if (doctorRoleId is null)
        {
            return [];
        }

        var doctors = await BuildPublicDoctorQuery(criteria, doctorRoleId.Value, null)
            .Select(profile => new PublicDoctorProjection(
                profile.Id,
                profile.UserId,
                profile.User!.FullName,
                profile.SpecialtyId,
                profile.Specialty != null ? profile.Specialty.Name : string.Empty,
                profile.Bio,
                profile.AvatarUrl))
            .ToListAsync(cancellationToken);

        return await AttachNextAvailableSlotsAsync(doctors, criteria, now, cancellationToken);
    }

    public async Task<PublicDoctorRecord?> GetByIdAsync(
        Guid doctorProfileId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var doctorRoleId = await GetDoctorRoleIdAsync(cancellationToken);

        if (doctorRoleId is null)
        {
            return null;
        }

        var doctors = await BuildPublicDoctorQuery(
                new PublicDoctorSearchCriteria(null, null, null, null),
                doctorRoleId.Value,
                doctorProfileId)
            .Select(profile => new PublicDoctorProjection(
                profile.Id,
                profile.UserId,
                profile.User!.FullName,
                profile.SpecialtyId,
                profile.Specialty != null ? profile.Specialty.Name : string.Empty,
                profile.Bio,
                profile.AvatarUrl))
            .ToListAsync(cancellationToken);

        var records = await AttachNextAvailableSlotsAsync(
            doctors,
            new PublicDoctorSearchCriteria(null, null, null, null),
            now,
            cancellationToken);

        return records.SingleOrDefault();
    }

    private Task<Guid?> GetDoctorRoleIdAsync(CancellationToken cancellationToken)
    {
        return dbContext.Roles
            .AsNoTracking()
            .Where(role => !role.IsDeleted && role.Name == DoctorRoleName)
            .Select(role => (Guid?)role.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private IQueryable<DoctorProfile> BuildPublicDoctorQuery(
        PublicDoctorSearchCriteria criteria,
        Guid doctorRoleId,
        Guid? doctorProfileId)
    {
        var doctors = dbContext.DoctorProfiles
            .AsNoTracking()
            .Where(profile =>
                !profile.IsDeleted &&
                profile.PublicProfileApproved &&
                profile.PublicProfileReviewStatus == DoctorProfileReviewStatus.Approved &&
                profile.User != null &&
                !profile.User.IsDeleted &&
                profile.User.Status == AccountStatus.Active &&
                profile.User.Roles.Any(userRole => userRole.RoleId == doctorRoleId));

        if (criteria.SpecialtyId.HasValue)
        {
            doctors = doctors.Where(profile => profile.SpecialtyId == criteria.SpecialtyId.Value);
        }

        if (doctorProfileId.HasValue)
        {
            doctors = doctors.Where(profile => profile.Id == doctorProfileId.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Keyword))
        {
            var keyword = criteria.Keyword.Trim();
            doctors = doctors.Where(profile =>
                profile.User!.FullName.Contains(keyword) ||
                (profile.Bio != null && profile.Bio.Contains(keyword)));
        }

        return doctors;
    }

    private async Task<IReadOnlyList<PublicDoctorRecord>> AttachNextAvailableSlotsAsync(
        IReadOnlyList<PublicDoctorProjection> doctors,
        PublicDoctorSearchCriteria criteria,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (doctors.Count == 0)
        {
            return [];
        }

        var doctorProfileIds = doctors
            .Select(doctor => doctor.DoctorProfileId)
            .ToList();

        var slotQuery = dbContext.DoctorScheduleSlots
            .AsNoTracking()
            .Where(slot =>
                doctorProfileIds.Contains(slot.DoctorProfileId) &&
                !slot.IsDeleted &&
                slot.Status == ScheduleSlotStatus.Available &&
                slot.StartTime > now);

        if (criteria.AvailableFrom.HasValue)
        {
            slotQuery = slotQuery.Where(slot => slot.StartTime >= criteria.AvailableFrom.Value);
        }

        if (criteria.AvailableTo.HasValue)
        {
            slotQuery = slotQuery.Where(slot => slot.StartTime < criteria.AvailableTo.Value);
        }

        var slots = await slotQuery
            .Select(slot => new PublicDoctorSlotProjection(
                slot.DoctorProfileId,
                slot.StartTime,
                slot.EndTime))
            .ToListAsync(cancellationToken);

        var nextSlotByDoctorId = slots
            .GroupBy(slot => slot.DoctorProfileId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(slot => slot.StartTime).First());

        return doctors
            .Select(doctor => new PublicDoctorRecord(
                doctor.DoctorProfileId,
                doctor.UserId,
                doctor.FullName,
                doctor.SpecialtyId,
                doctor.SpecialtyName,
                doctor.Bio,
                doctor.AvatarUrl,
                nextSlotByDoctorId.TryGetValue(doctor.DoctorProfileId, out var slot) ? slot.StartTime : null,
                slot?.EndTime))
            .OrderBy(doctor => doctor.NextAvailableSlotStartTime.HasValue ? 0 : 1)
            .ThenBy(doctor => doctor.NextAvailableSlotStartTime ?? DateTimeOffset.MaxValue)
            .ThenBy(doctor => doctor.FullName)
            .ToList();
    }

    private sealed record PublicDoctorProjection(
        Guid DoctorProfileId,
        Guid UserId,
        string FullName,
        Guid SpecialtyId,
        string SpecialtyName,
        string? Bio,
        string? AvatarUrl);

    private sealed record PublicDoctorSlotProjection(
        Guid DoctorProfileId,
        DateTimeOffset StartTime,
        DateTimeOffset EndTime);
}
