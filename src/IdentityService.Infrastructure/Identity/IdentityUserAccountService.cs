using IdentityService.Application;
using IdentityService.Application.SignIn;
using IdentityService.Application.SignUp;
using IdentityService.Contracts;
using IdentityService.Contracts.SignUp;
using IdentityService.Domain.Identity;
using IdentityService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdentityService.Infrastructure.Identity;

public sealed class IdentityUserAccountService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IdentityServiceDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<IdentityUserAccountService> logger) : IUserAccountService
{
    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByEmailAsync(email);

        return user is not null;
    }

    public async Task<CreateUserAccountResult> CreateTenantAdminAsync(
        string fullName,
        string email,
        string password,
        DateTimeOffset termsAcceptedAt,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureRoleExistsAsync(UserRoles.TenantAdmin, cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            CreatedAt = timeProvider.GetUtcNow(),
        };

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync(cancellationToken);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            UserName = email,
            Email = email,
            EmailConfirmed = false,
            CreatedAt = termsAcceptedAt,
            TermsAcceptedAt = termsAcceptedAt,
            TenantId = tenant.Id,
        };

        var createResult = await userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
        {
            logger.LogWarning(
                "Identity user creation failed. ErrorCount: {ErrorCount}.",
                createResult.Errors.Count());
            await transaction.RollbackAsync(cancellationToken);
            return CreateUserAccountResult.Failure(ToAuthErrors(createResult.Errors));
        }

        var roleResult = await userManager.AddToRoleAsync(user, UserRoles.TenantAdmin);

        if (!roleResult.Succeeded)
        {
            logger.LogWarning(
                "Identity role assignment failed. UserId: {UserId}. Role: {Role}. ErrorCount: {ErrorCount}.",
                user.Id,
                UserRoles.TenantAdmin,
                roleResult.Errors.Count());
            await transaction.RollbackAsync(cancellationToken);
            return CreateUserAccountResult.Failure(ToAuthErrors(roleResult.Errors));
        }

        foreach (var permission in Permissions.All)
        {
            dbContext.UserPermissions.Add(new UserPermission
            {
                UserId = user.Id,
                Permission = permission,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Identity user created. UserId: {UserId}. TenantId: {TenantId}. Role: {Role}. PermissionCount: {PermissionCount}.",
            user.Id,
            user.TenantId,
            UserRoles.TenantAdmin,
            Permissions.All.Count);

        return CreateUserAccountResult.Success(new SignUpResponse(
            user.Id,
            user.Email!,
            user.FullName,
            UserRoles.TenantAdmin,
            user.IsDisabled ? "Disabled" : "Active"));
    }

    public async Task<CredentialsValidationResult> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            logger.LogWarning("Credentials validation failed. FailureReason: {FailureReason}.", SignInFailureReason.InvalidCredentials);
            return CredentialsValidationResult.Failure(SignInFailureReason.InvalidCredentials);
        }

        if (user.IsDisabled)
        {
            logger.LogWarning("Credentials validation failed. UserId: {UserId}. FailureReason: {FailureReason}.", user.Id, SignInFailureReason.Disabled);
            return CredentialsValidationResult.Failure(SignInFailureReason.Disabled);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            logger.LogWarning("Credentials validation failed. UserId: {UserId}. FailureReason: {FailureReason}.", user.Id, SignInFailureReason.LockedOut);
            return CredentialsValidationResult.Failure(SignInFailureReason.LockedOut);
        }

        var passwordMatches = await userManager.CheckPasswordAsync(user, password);

        if (!passwordMatches)
        {
            if (userManager.SupportsUserLockout)
            {
                await userManager.AccessFailedAsync(user);
            }

            logger.LogWarning("Credentials validation failed. UserId: {UserId}. FailureReason: {FailureReason}.", user.Id, SignInFailureReason.InvalidCredentials);
            return CredentialsValidationResult.Failure(SignInFailureReason.InvalidCredentials);
        }

        if (userManager.SupportsUserLockout)
        {
            await userManager.ResetAccessFailedCountAsync(user);
        }

        var roles = await userManager.GetRolesAsync(user);

        var permissions = await dbContext.UserPermissions
            .AsNoTracking()
            .Where(permission => permission.UserId == user.Id)
            .Select(permission => permission.Permission)
            .ToArrayAsync(cancellationToken);

        logger.LogInformation(
            "Credentials validation succeeded. UserId: {UserId}. TenantId: {TenantId}. RoleCount: {RoleCount}. PermissionCount: {PermissionCount}.",
            user.Id,
            user.TenantId,
            roles.Count,
            permissions.Length);

        return CredentialsValidationResult.Success(new AuthenticatedUser(
            user.Id,
            user.Email!,
            user.FullName,
            user.TenantId,
            roles.ToArray(),
            permissions));
    }

    private async Task EnsureRoleExistsAsync(string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));

        if (!result.Succeeded)
        {
            logger.LogError(
                "Required identity role creation failed. Role: {Role}. ErrorCount: {ErrorCount}.",
                roleName,
                result.Errors.Count());
            throw new InvalidOperationException($"Unable to create required role '{roleName}'.");
        }

        logger.LogInformation("Required identity role created. Role: {Role}.", roleName);
    }

    private static IReadOnlyCollection<AuthError> ToAuthErrors(IEnumerable<IdentityError> errors)
    {
        return errors
            .Select(error => new AuthError(error.Code, error.Description))
            .ToArray();
    }
}
