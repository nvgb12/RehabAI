using Microsoft.EntityFrameworkCore;
using RehabAI.Application.Doctors;
using RehabAI.Domain.Entities;
using RehabAI.Domain.Enums;
using RehabAI.Infrastructure.Database;

namespace RehabAI.Infrastructure.Doctors;

public sealed class EfDoctorAccountRepository(AppDbContext dbContext) : IDoctorAccountRepository
{
    private const decimal FallbackCommissionRate = 15m;
    private const string DefaultCommissionRateSettingKey = "Platform.DefaultCommissionRate";

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken);
    }

    public async Task<Guid?> GetRoleIdByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        return await dbContext.Roles
            .Where(role => role.Name == roleName)
            .Select(role => (Guid?)role.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> SpecialtyExistsAsync(Guid specialtyId, CancellationToken cancellationToken = default)
    {
        return dbContext.Specialties.AnyAsync(
            specialty => specialty.Id == specialtyId && specialty.IsActive,
            cancellationToken);
    }

    public async Task<decimal> GetDefaultCommissionRateAsync(CancellationToken cancellationToken = default)
    {
        var settingValue = await dbContext.SystemSettings
            .Where(setting => setting.SettingKey == DefaultCommissionRateSettingKey)
            .Select(setting => setting.SettingValue)
            .SingleOrDefaultAsync(cancellationToken);

        return decimal.TryParse(settingValue, out var commissionRate)
            ? commissionRate
            : FallbackCommissionRate;
    }

    public async Task<CreatedDoctorAccountResult> CreateDoctorAccountAsync(
        CreatedDoctorAccount account,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            FullName = account.FullName,
            Email = account.Email,
            PhoneNumber = account.PhoneNumber,
            PasswordHash = null,
            Status = AccountStatus.PendingPasswordSetup,
            EmailConfirmed = true,
            CreatedAt = now
        };
        var doctorProfile = new DoctorProfile
        {
            UserId = user.Id,
            SpecialtyId = account.SpecialtyId,
            Bio = account.Bio,
            PublicProfileApproved = false,
            PublicProfileReviewStatus = DoctorProfileReviewStatus.Draft,
            CommissionRate = account.CommissionRate,
            CreatedAt = now
        };
        var userRole = new UserRoleAssignment
        {
            UserId = user.Id,
            RoleId = account.DoctorRoleId
        };
        var invitationToken = new UserToken
        {
            UserId = user.Id,
            TokenType = UserTokenType.DoctorInvitation,
            TokenHash = account.InvitationTokenHash,
            ExpiresAt = account.InvitationTokenExpiresAt,
            CreatedAt = now
        };
        var emailLog = new EmailLog
        {
            UserId = user.Id,
            ToEmail = account.Email,
            Subject = account.EmailSubject,
            TemplateName = account.EmailTemplateName,
            Status = "Pending",
            CreatedAt = now
        };
        var auditLog = new AuditLog
        {
            ActorUserId = null,
            Action = "DoctorAccountCreated",
            EntityName = nameof(DoctorProfile),
            EntityId = doctorProfile.Id,
            MetadataJson = $$"""{"userId":"{{user.Id}}","email":"{{account.Email}}","status":"PendingPasswordSetup"}""",
            CreatedAt = now
        };

        dbContext.Users.Add(user);
        dbContext.DoctorProfiles.Add(doctorProfile);
        dbContext.UserRoles.Add(userRole);
        dbContext.UserTokens.Add(invitationToken);
        dbContext.EmailLogs.Add(emailLog);
        dbContext.AuditLogs.Add(auditLog);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreatedDoctorAccountResult(user.Id, doctorProfile.Id, emailLog.Id);
    }

    public async Task<IReadOnlyList<AdminDoctorRecord>> GetAdminDoctorsAsync(
        CancellationToken cancellationToken = default)
    {
        return await BuildAdminDoctorQuery()
            .OrderByDescending(profile => profile.CreatedAt)
            .ThenBy(profile => profile.User!.FullName)
            .Select(ToAdminDoctorRecordExpression())
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminDoctorRecord?> GetAdminDoctorByIdAsync(
        Guid doctorProfileId,
        CancellationToken cancellationToken = default)
    {
        return await BuildAdminDoctorQuery()
            .Where(profile => profile.Id == doctorProfileId)
            .Select(ToAdminDoctorRecordExpression())
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<AdminDoctorRecord?> ApprovePublicProfileAsync(
        Guid doctorProfileId,
        Guid adminUserId,
        DateTimeOffset reviewedAt,
        CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.DoctorProfiles
            .SingleOrDefaultAsync(
                doctorProfile =>
                    doctorProfile.Id == doctorProfileId &&
                    !doctorProfile.IsDeleted,
                cancellationToken);

        if (profile is null)
        {
            return null;
        }

        profile.PublicProfileReviewStatus = DoctorProfileReviewStatus.Approved;
        profile.PublicProfileApproved = true;
        profile.ReviewedAt = reviewedAt;
        profile.ReviewedByAdminId = adminUserId;
        profile.PublicProfileRejectionReason = null;
        profile.UpdatedAt = reviewedAt;

        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminUserId,
            Action = "DoctorPublicProfileApproved",
            EntityName = nameof(DoctorProfile),
            EntityId = profile.Id,
            MetadataJson = $$"""{"reviewStatus":"Approved","doctorProfileId":"{{profile.Id}}"}""",
            CreatedAt = reviewedAt
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetAdminDoctorByIdAsync(doctorProfileId, cancellationToken);
    }

    public async Task<AdminDoctorRecord?> RejectPublicProfileAsync(
        Guid doctorProfileId,
        Guid adminUserId,
        string rejectionReason,
        DateTimeOffset reviewedAt,
        CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.DoctorProfiles
            .SingleOrDefaultAsync(
                doctorProfile =>
                    doctorProfile.Id == doctorProfileId &&
                    !doctorProfile.IsDeleted,
                cancellationToken);

        if (profile is null)
        {
            return null;
        }

        profile.PublicProfileReviewStatus = DoctorProfileReviewStatus.Rejected;
        profile.PublicProfileApproved = false;
        profile.ReviewedAt = reviewedAt;
        profile.ReviewedByAdminId = adminUserId;
        profile.PublicProfileRejectionReason = rejectionReason;
        profile.UpdatedAt = reviewedAt;

        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminUserId,
            Action = "DoctorPublicProfileRejected",
            EntityName = nameof(DoctorProfile),
            EntityId = profile.Id,
            MetadataJson = $$"""{"reviewStatus":"Rejected","doctorProfileId":"{{profile.Id}}"}""",
            CreatedAt = reviewedAt
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetAdminDoctorByIdAsync(doctorProfileId, cancellationToken);
    }

    private IQueryable<DoctorProfile> BuildAdminDoctorQuery()
    {
        return dbContext.DoctorProfiles
            .AsNoTracking()
            .Where(profile => !profile.IsDeleted && profile.User != null && !profile.User.IsDeleted);
    }

    private static System.Linq.Expressions.Expression<Func<DoctorProfile, AdminDoctorRecord>> ToAdminDoctorRecordExpression()
    {
        return profile => new AdminDoctorRecord(
            profile.Id,
            profile.UserId,
            profile.User!.FullName,
            profile.User.Email,
            profile.User.PhoneNumber,
            profile.User.Status,
            profile.User.EmailConfirmed,
            profile.SpecialtyId,
            profile.Specialty!.Name,
            profile.Bio,
            profile.AvatarUrl,
            profile.PublicProfileApproved,
            profile.PublicProfileReviewStatus,
            profile.SubmittedForReviewAt,
            profile.ReviewedAt,
            profile.ReviewedByAdminId,
            profile.PublicProfileRejectionReason,
            profile.CreatedAt,
            profile.UpdatedAt,
            profile.IsDeleted);
    }

    public async Task MarkInvitationEmailSentAsync(Guid emailLogId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var emailLog = await dbContext.EmailLogs.SingleAsync(log => log.Id == emailLogId, cancellationToken);

        emailLog.Status = "Sent";
        emailLog.SentAt = now;
        emailLog.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkInvitationEmailFailedAsync(
        Guid emailLogId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var emailLog = await dbContext.EmailLogs.SingleAsync(log => log.Id == emailLogId, cancellationToken);

        emailLog.Status = "Failed";
        emailLog.ErrorMessage = errorMessage;
        emailLog.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
