using Microsoft.EntityFrameworkCore;
using RehabAI.Application.PatientProfiles;
using RehabAI.Infrastructure.Database;

namespace RehabAI.Infrastructure.PatientProfiles;

public sealed class EfPatientProfileRepository(AppDbContext dbContext) : IPatientProfileRepository
{
    public Task<PatientProfileRecord?> GetByIdAsync(
        Guid patientProfileId,
        CancellationToken cancellationToken = default)
    {
        return GetByIdInternalAsync(patientProfileId, cancellationToken);
    }

    public async Task<PatientProfileRecord?> UpdateAsync(
        Guid patientProfileId,
        UpdatePatientProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.PatientProfiles
            .Include(profile => profile.User)
            .SingleOrDefaultAsync(
                profile =>
                    profile.Id == patientProfileId &&
                    !profile.IsDeleted &&
                    profile.User != null &&
                    !profile.User.IsDeleted,
                cancellationToken);

        if (profile is null)
        {
            return null;
        }

        profile.User!.FullName = command.FullName!;
        profile.User.PhoneNumber = command.PhoneNumber;
        profile.User.UpdatedAt = DateTimeOffset.UtcNow;
        profile.DateOfBirth = command.DateOfBirth;
        profile.Gender = command.Gender;
        profile.Address = command.Address;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToRecord(profile);
    }

    public Task<PatientProfileRecord?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return GetByUserIdInternalAsync(userId, cancellationToken);
    }

    public async Task<string?> UpdateProfileImageAsync(
        Guid patientProfileId,
        string profileImageUrl,
        CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.PatientProfiles
            .SingleOrDefaultAsync(
                profile => profile.Id == patientProfileId && !profile.IsDeleted,
                cancellationToken);

        if (profile is null)
        {
            return null;
        }

        profile.ProfileImageUrl = profileImageUrl;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return profile.ProfileImageUrl;
    }

    private async Task<PatientProfileRecord?> GetByIdInternalAsync(
        Guid patientProfileId,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.PatientProfiles
            .Include(profile => profile.User)
            .SingleOrDefaultAsync(
                profile =>
                    profile.Id == patientProfileId &&
                    !profile.IsDeleted &&
                    profile.User != null &&
                    !profile.User.IsDeleted,
                cancellationToken);

        return profile is null ? null : ToRecord(profile);
    }

    private async Task<PatientProfileRecord?> GetByUserIdInternalAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.PatientProfiles
            .Include(profile => profile.User)
            .SingleOrDefaultAsync(
                profile =>
                    profile.UserId == userId &&
                    !profile.IsDeleted &&
                    profile.User != null &&
                    !profile.User.IsDeleted,
                cancellationToken);

        return profile is null ? null : ToRecord(profile);
    }

    private static PatientProfileRecord ToRecord(RehabAI.Domain.Entities.PatientProfile profile)
    {
        return new PatientProfileRecord(
            profile.Id,
            profile.UserId,
            profile.User?.FullName ?? string.Empty,
            profile.User?.Email ?? string.Empty,
            profile.User?.PhoneNumber,
            profile.DateOfBirth,
            profile.Gender,
            profile.Address,
            profile.ProfileImageUrl);
    }
}
