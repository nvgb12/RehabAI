using Microsoft.EntityFrameworkCore;
using RehabAI.Domain.Enums;
using RehabAI.Infrastructure.Database;

namespace RehabAI.Api.Authorization;

public interface IEndpointAccessService
{
    Task<bool> PatientProfileBelongsToUserAsync(
        Guid userId,
        Guid patientProfileId,
        CancellationToken cancellationToken = default);
    Task<Guid?> GetPatientProfileIdForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<bool> AppointmentBelongsToUserAsync(
        Guid userId,
        Guid appointmentId,
        CancellationToken cancellationToken = default);
    Task<bool> OrderBelongsToUserAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken = default);
    Task<bool> CanManageDoctorProfileAsync(
        Guid userId,
        Guid doctorProfileId,
        CancellationToken cancellationToken = default);
}

public sealed class EndpointAccessService(AppDbContext dbContext) : IEndpointAccessService
{
    private static readonly string[] StaffOrAdminRoles =
    [
        AccessPolicies.AdminRole,
        AccessPolicies.AuthorizedInternalStaffRole,
        AccessPolicies.SupportStaffRole,
        AccessPolicies.VerificationAdminRole
    ];

    public Task<bool> PatientProfileBelongsToUserAsync(
        Guid userId,
        Guid patientProfileId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.PatientProfiles
            .AsNoTracking()
            .AnyAsync(
                profile =>
                    profile.Id == patientProfileId &&
                    profile.UserId == userId &&
                    !profile.IsDeleted,
                cancellationToken);
    }

    public async Task<Guid?> GetPatientProfileIdForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PatientProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == userId && !profile.IsDeleted)
            .Select(profile => (Guid?)profile.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> AppointmentBelongsToUserAsync(
        Guid userId,
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Appointments
            .AsNoTracking()
            .AnyAsync(
                appointment =>
                    appointment.Id == appointmentId &&
                    appointment.PatientId == userId &&
                    !appointment.IsDeleted,
                cancellationToken);
    }

    public Task<bool> OrderBelongsToUserAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Orders
            .AsNoTracking()
            .AnyAsync(
                order =>
                    order.Id == orderId &&
                    order.UserId == userId &&
                    !order.IsDeleted,
                cancellationToken);
    }

    public async Task<bool> CanManageDoctorProfileAsync(
        Guid userId,
        Guid doctorProfileId,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(item =>
                item.Id == userId &&
                !item.IsDeleted &&
                item.Status == AccountStatus.Active)
            .Select(item => new
            {
                Roles = item.Roles
                    .Where(userRole => userRole.Role != null && !userRole.Role.IsDeleted)
                    .Select(userRole => userRole.Role!.Name)
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return false;
        }

        if (user.Roles.Any(role => StaffOrAdminRoles.Contains(role, StringComparer.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (!user.Roles.Contains(AccessPolicies.DoctorRole, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        return await dbContext.DoctorProfiles
            .AsNoTracking()
            .AnyAsync(
                profile =>
                    profile.Id == doctorProfileId &&
                    profile.UserId == userId &&
                    !profile.IsDeleted,
                cancellationToken);
    }
}
