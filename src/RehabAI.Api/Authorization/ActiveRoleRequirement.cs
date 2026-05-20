using Microsoft.AspNetCore.Authorization;

namespace RehabAI.Api.Authorization;

public sealed class ActiveRoleRequirement(params string[] allowedRoles) : IAuthorizationRequirement
{
    public IReadOnlySet<string> AllowedRoles { get; } = allowedRoles
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
