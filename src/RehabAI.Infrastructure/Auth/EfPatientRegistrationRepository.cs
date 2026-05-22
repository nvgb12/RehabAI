using Microsoft.EntityFrameworkCore;
using RehabAI.Application.Auth;
using RehabAI.Domain.Entities;
using RehabAI.Domain.Enums;
using RehabAI.Infrastructure.Database;

namespace RehabAI.Infrastructure.Auth;

public sealed class EfPatientRegistrationRepository(AppDbContext dbContext) :
    IPatientRegistrationRepository,
    IUserAuthenticationRepository
{
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

    public async Task<UserAuthenticationRecord?> GetUserForLoginAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .Where(user => user.Email == normalizedEmail)
            .Select(user => new
            {
                user.Id,
                user.Email,
                user.FullName,
                user.PasswordHash,
                Status = (int)user.Status,
                Roles = user.Roles
                    .Select(userRole => userRole.Role != null ? userRole.Role.Name : string.Empty)
                    .Where(roleName => roleName != string.Empty)
                    .OrderBy(roleName => roleName)
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var patientProfileId = await dbContext.PatientProfiles
            .Where(profile => profile.UserId == user.Id && !profile.IsDeleted)
            .Select(profile => (Guid?)profile.Id)
            .SingleOrDefaultAsync(cancellationToken);

        var doctorProfileId = await dbContext.DoctorProfiles
            .Where(profile => profile.UserId == user.Id && !profile.IsDeleted)
            .Select(profile => (Guid?)profile.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return new UserAuthenticationRecord(
            user.Id,
            user.Email,
            user.FullName,
            user.PasswordHash,
            user.Status,
            user.Roles,
            patientProfileId,
            doctorProfileId);
    }

    public async Task<PendingPatientRegistrationResult> CreatePendingPatientAsync(
        PendingPatientRegistration registration,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            FullName = registration.FullName,
            Email = registration.Email,
            PhoneNumber = registration.PhoneNumber,
            PasswordHash = registration.PasswordHash,
            Status = AccountStatus.PendingEmail,
            EmailConfirmed = false,
            CreatedAt = now
        };

        var patientProfile = new PatientProfile
        {
            UserId = user.Id,
            CreatedAt = now
        };

        var userRole = new UserRoleAssignment
        {
            UserId = user.Id,
            RoleId = registration.PatientRoleId
        };

        var verificationToken = new UserToken
        {
            UserId = user.Id,
            TokenType = UserTokenType.EmailVerification,
            TokenHash = registration.VerificationTokenHash,
            ExpiresAt = registration.VerificationTokenExpiresAt,
            CreatedAt = now
        };

        var emailLog = new EmailLog
        {
            UserId = user.Id,
            ToEmail = registration.Email,
            Subject = registration.EmailSubject,
            TemplateName = registration.EmailTemplateName,
            Status = "Pending",
            CreatedAt = now
        };

        dbContext.Users.Add(user);
        dbContext.PatientProfiles.Add(patientProfile);
        dbContext.UserRoles.Add(userRole);
        dbContext.UserTokens.Add(verificationToken);
        dbContext.EmailLogs.Add(emailLog);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new PendingPatientRegistrationResult(user.Id, emailLog.Id);
    }

    public async Task<IReadOnlyList<EmailVerificationTokenRecord>> GetEmailVerificationTokensAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.UserTokens
            .Where(token =>
                token.TokenType == UserTokenType.EmailVerification &&
                token.User != null &&
                token.User.Email == normalizedEmail)
            .Select(token => new EmailVerificationTokenRecord(
                token.UserId,
                token.Id,
                token.User!.Email,
                token.TokenHash,
                token.ExpiresAt,
                token.UsedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task CompleteEmailVerificationAsync(Guid userId, Guid tokenId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var user = await dbContext.Users.SingleAsync(user => user.Id == userId, cancellationToken);
        var token = await dbContext.UserTokens.SingleAsync(token => token.Id == tokenId, cancellationToken);

        token.UsedAt = now;
        token.UpdatedAt = now;
        user.EmailConfirmed = true;
        user.Status = AccountStatus.Active;
        user.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DoctorInvitationTokenRecord>> GetDoctorInvitationTokensAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.UserTokens
            .Where(token =>
                token.TokenType == UserTokenType.DoctorInvitation &&
                token.User != null &&
                token.User.Email == normalizedEmail)
            .Select(token => new DoctorInvitationTokenRecord(
                token.UserId,
                token.Id,
                token.User!.Email,
                token.TokenHash,
                token.ExpiresAt,
                token.UsedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task CompleteDoctorPasswordSetupAsync(
        Guid userId,
        Guid tokenId,
        string passwordHash,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var user = await dbContext.Users.SingleAsync(user => user.Id == userId, cancellationToken);
        var token = await dbContext.UserTokens.SingleAsync(token => token.Id == tokenId, cancellationToken);

        token.UsedAt = now;
        token.UpdatedAt = now;
        user.PasswordHash = passwordHash;
        user.Status = AccountStatus.Active;
        user.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PasswordResetUserRecord?> GetEligiblePasswordResetUserAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .Where(user =>
                user.Email == normalizedEmail &&
                !user.IsDeleted &&
                user.Status == AccountStatus.Active &&
                user.EmailConfirmed)
            .Select(user => new PasswordResetUserRecord(
                user.Id,
                user.Email,
                user.FullName))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid?> CreatePasswordResetAsync(
        PendingPasswordReset reset,
        CancellationToken cancellationToken = default)
    {
        var userExists = await dbContext.Users.AnyAsync(
            user =>
                user.Id == reset.UserId &&
                user.Email == reset.Email &&
                !user.IsDeleted &&
                user.Status == AccountStatus.Active &&
                user.EmailConfirmed,
            cancellationToken);

        if (!userExists)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var token = new UserToken
        {
            UserId = reset.UserId,
            TokenType = UserTokenType.PasswordReset,
            TokenHash = reset.TokenHash,
            ExpiresAt = reset.ExpiresAt,
            CreatedAt = now
        };

        var emailLog = new EmailLog
        {
            UserId = reset.UserId,
            ToEmail = reset.Email,
            Subject = reset.EmailSubject,
            TemplateName = reset.EmailTemplateName,
            Status = string.IsNullOrWhiteSpace(reset.DevelopmentPayloadJson) ? "Pending" : "DevelopmentLogged",
            MetadataJson = reset.DevelopmentPayloadJson,
            CreatedAt = now
        };

        dbContext.UserTokens.Add(token);
        dbContext.EmailLogs.Add(emailLog);

        await dbContext.SaveChangesAsync(cancellationToken);

        return emailLog.Id;
    }

    public async Task<IReadOnlyList<PasswordResetTokenRecord>> GetPasswordResetTokensAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.UserTokens
            .Where(token =>
                token.TokenType == UserTokenType.PasswordReset &&
                !token.IsDeleted &&
                token.User != null &&
                !token.User.IsDeleted &&
                token.User.Email == normalizedEmail)
            .Select(token => new PasswordResetTokenRecord(
                token.UserId,
                token.Id,
                token.User!.Email,
                token.TokenHash,
                token.ExpiresAt,
                token.UsedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task CompletePasswordResetAsync(
        Guid userId,
        Guid tokenId,
        string passwordHash,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var user = await dbContext.Users.SingleAsync(user => user.Id == userId && !user.IsDeleted, cancellationToken);
        var token = await dbContext.UserTokens.SingleAsync(
            token =>
                token.Id == tokenId &&
                token.UserId == userId &&
                token.TokenType == UserTokenType.PasswordReset &&
                !token.IsDeleted,
            cancellationToken);

        token.UsedAt = now;
        token.UpdatedAt = now;
        user.PasswordHash = passwordHash;
        user.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkVerificationEmailSentAsync(Guid emailLogId, CancellationToken cancellationToken = default)
    {
        await MarkEmailSentAsync(emailLogId, cancellationToken);
    }

    public async Task MarkEmailSentAsync(Guid emailLogId, CancellationToken cancellationToken = default)
    {
        var emailLog = await dbContext.EmailLogs.SingleAsync(log => log.Id == emailLogId, cancellationToken);
        emailLog.Status = emailLog.MetadataJson is null ? "Sent" : "DevelopmentLogged";
        emailLog.SentAt = DateTimeOffset.UtcNow;
        emailLog.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkVerificationEmailFailedAsync(
        Guid emailLogId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        await MarkEmailFailedAsync(emailLogId, errorMessage, cancellationToken);
    }

    public async Task MarkEmailFailedAsync(
        Guid emailLogId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        var emailLog = await dbContext.EmailLogs.SingleAsync(log => log.Id == emailLogId, cancellationToken);
        emailLog.Status = "Failed";
        emailLog.ErrorMessage = errorMessage;
        emailLog.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
