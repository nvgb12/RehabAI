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
        var query = BuildPublicDoctorQuery(criteria, now, null);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<PublicDoctorRecord?> GetByIdAsync(
        Guid doctorProfileId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var query = BuildPublicDoctorQuery(
            new PublicDoctorSearchCriteria(null, null, null, null),
            now,
            doctorProfileId);

        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    private IQueryable<PublicDoctorRecord> BuildPublicDoctorQuery(
        PublicDoctorSearchCriteria criteria,
        DateTimeOffset now,
        Guid? doctorProfileId)
    {
        var slotQuery = dbContext.DoctorScheduleSlots
            .Where(slot =>
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

        var nextSlotStartTimes = slotQuery
            .GroupBy(slot => slot.DoctorProfileId)
            .Select(group => new
            {
                DoctorProfileId = group.Key,
                StartTime = group.Min(slot => slot.StartTime)
            });

        var nextSlots = slotQuery
            .Join(
                nextSlotStartTimes,
                slot => new { slot.DoctorProfileId, slot.StartTime },
                nextSlot => new { nextSlot.DoctorProfileId, nextSlot.StartTime },
                (slot, nextSlot) => new
                {
                    slot.DoctorProfileId,
                    slot.StartTime,
                    slot.EndTime
                });

        var doctors = dbContext.DoctorProfiles
            .Where(profile =>
                !profile.IsDeleted &&
                profile.PublicProfileApproved &&
                profile.User != null &&
                !profile.User.IsDeleted &&
                profile.User.Status == AccountStatus.Active &&
                profile.User.Roles.Any(userRole =>
                    userRole.Role != null &&
                    !userRole.Role.IsDeleted &&
                    userRole.Role.Name == DoctorRoleName));

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

        return doctors
            .Join(
                nextSlots,
                profile => profile.Id,
                slot => slot.DoctorProfileId,
                (profile, slot) => new
                {
                    Profile = profile,
                    Slot = slot
                })
            .OrderBy(doctor => doctor.Slot.StartTime)
            .ThenBy(doctor => doctor.Profile.User!.FullName)
            .Select(doctor => new PublicDoctorRecord(
                doctor.Profile.Id,
                doctor.Profile.UserId,
                doctor.Profile.User!.FullName,
                doctor.Profile.SpecialtyId,
                doctor.Profile.Specialty != null ? doctor.Profile.Specialty.Name : string.Empty,
                doctor.Profile.Bio,
                doctor.Profile.AvatarUrl,
                doctor.Slot.StartTime,
                doctor.Slot.EndTime));
    }
}
