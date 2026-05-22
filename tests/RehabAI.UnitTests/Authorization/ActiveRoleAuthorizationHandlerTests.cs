using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RehabAI.Api.Authorization;
using RehabAI.Domain.Entities;
using RehabAI.Domain.Enums;
using RehabAI.Infrastructure.Database;

namespace RehabAI.UnitTests.Authorization;

public class ActiveRoleAuthorizationHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenUserIsActivePatient_Succeeds()
    {
        await using var dbContext = CreateDbContext();
        var user = AddUser(dbContext, AccountStatus.Active, AccessPolicies.PatientRole);
        var handler = new ActiveRoleAuthorizationHandler(dbContext);
        var requirement = new ActiveRoleRequirement(AccessPolicies.PatientRole);
        var context = CreateContext(user.Id, requirement);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Theory]
    [InlineData(AccountStatus.PendingEmail)]
    [InlineData(AccountStatus.PendingPasswordSetup)]
    [InlineData(AccountStatus.Locked)]
    [InlineData(AccountStatus.Suspended)]
    [InlineData(AccountStatus.Deactivated)]
    public async Task HandleAsync_WhenUserIsNotActive_Fails(AccountStatus status)
    {
        await using var dbContext = CreateDbContext();
        var user = AddUser(dbContext, status, AccessPolicies.PatientRole);
        var handler = new ActiveRoleAuthorizationHandler(dbContext);
        var requirement = new ActiveRoleRequirement(AccessPolicies.PatientRole);
        var context = CreateContext(user.Id, requirement);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_WhenUserHasWrongRole_Fails()
    {
        await using var dbContext = CreateDbContext();
        var user = AddUser(dbContext, AccountStatus.Active, AccessPolicies.DoctorRole);
        var handler = new ActiveRoleAuthorizationHandler(dbContext);
        var requirement = new ActiveRoleRequirement(AccessPolicies.AdminRole);
        var context = CreateContext(user.Id, requirement);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_WhenAdminUsesPatientOnlyPolicy_Fails()
    {
        await using var dbContext = CreateDbContext();
        var user = AddUser(dbContext, AccountStatus.Active, AccessPolicies.AdminRole);
        var handler = new ActiveRoleAuthorizationHandler(dbContext);
        var requirement = new ActiveRoleRequirement(AccessPolicies.PatientRole);
        var context = CreateContext(user.Id, requirement);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsActiveDoctor_SucceedsDoctorPolicy()
    {
        await using var dbContext = CreateDbContext();
        var user = AddUser(dbContext, AccountStatus.Active, AccessPolicies.DoctorRole);
        var handler = new ActiveRoleAuthorizationHandler(dbContext);
        var requirement = new ActiveRoleRequirement(AccessPolicies.DoctorRole);
        var context = CreateContext(user.Id, requirement);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_WhenPatientUsesDoctorPolicy_Fails()
    {
        await using var dbContext = CreateDbContext();
        var user = AddUser(dbContext, AccountStatus.Active, AccessPolicies.PatientRole);
        var handler = new ActiveRoleAuthorizationHandler(dbContext);
        var requirement = new ActiveRoleRequirement(AccessPolicies.DoctorRole);
        var context = CreateContext(user.Id, requirement);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static User AddUser(AppDbContext dbContext, AccountStatus status, string roleName)
    {
        var user = new User
        {
            FullName = "Test User",
            Email = $"{Guid.NewGuid():N}@test.local",
            Status = status,
            EmailConfirmed = status == AccountStatus.Active
        };
        var role = new Role
        {
            Name = roleName,
            Description = roleName
        };
        var userRole = new UserRoleAssignment
        {
            UserId = user.Id,
            RoleId = role.Id,
            User = user,
            Role = role
        };

        user.Roles.Add(userRole);
        role.Users.Add(userRole);
        dbContext.Users.Add(user);
        dbContext.Roles.Add(role);
        dbContext.UserRoles.Add(userRole);
        dbContext.SaveChanges();

        return user;
    }

    private static AuthorizationHandlerContext CreateContext(
        Guid userId,
        IAuthorizationRequirement requirement)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", userId.ToString())],
            "TestAuth"));

        return new AuthorizationHandlerContext([requirement], principal, null);
    }
}
