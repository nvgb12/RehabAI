using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RehabAI.Domain.Enums;
using RehabAI.Infrastructure.Database;

namespace RehabAI.Api.Authorization;

public sealed class ActiveRoleAuthorizationHandler(AppDbContext dbContext) :
    AuthorizationHandler<ActiveRoleRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveRoleRequirement requirement)
    {
        var userId = GetCurrentUserId(context.User);

        if (userId is null)
        {
            return;
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .Where(item => item.Id == userId.Value && !item.IsDeleted)
            .Select(item => new
            {
                item.Status,
                Roles = item.Roles
                    .Where(userRole => userRole.Role != null && !userRole.Role.IsDeleted)
                    .Select(userRole => userRole.Role!.Name)
                    .ToList()
            })
            .SingleOrDefaultAsync();

        if (user is null || user.Status != AccountStatus.Active)
        {
            return;
        }

        if (user.Roles.Any(role => requirement.AllowedRoles.Contains(role)))
        {
            context.Succeed(requirement);
        }
    }

    private static Guid? GetCurrentUserId(ClaimsPrincipal user)
    {
        var claimValue =
            user.FindFirstValue("sub") ??
            user.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }
}
