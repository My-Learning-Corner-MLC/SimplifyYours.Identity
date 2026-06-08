using IdentityService.Application;
using IdentityService.Application.SignIn;
using IdentityService.Application.SignUp;
using IdentityService.Contracts;
using IdentityService.Contracts.SignUp;
using IdentityService.Domain.Identity;
using IdentityService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace IdentityService.Infrastructure.Identity;

public sealed class IdentityUserAccountService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    ILogger<IdentityUserAccountService> logger) : IUserAccountService
{
    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByEmailAsync(email);

        return user is not null;
    }

    public async Task<CreateUserAccountResult> CreateNormalUserAsync(
        string fullName,
        string email,
        string password,
        DateTimeOffset termsAcceptedAt,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureRoleExistsAsync(UserRoles.NormalUser, cancellationToken);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            UserName = email,
            Email = email,
            EmailConfirmed = false,
            CreatedAt = termsAcceptedAt,
            TermsAcceptedAt = termsAcceptedAt
        };

        var createResult = await userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
        {
            logger.LogWarning(
                "Identity user creation failed. ErrorCount: {ErrorCount}.",
                createResult.Errors.Count());
            return CreateUserAccountResult.Failure(ToAuthErrors(createResult.Errors));
        }

        var roleResult = await userManager.AddToRoleAsync(user, UserRoles.NormalUser);

        if (!roleResult.Succeeded)
        {
            logger.LogWarning(
                "Identity role assignment failed. UserId: {UserId}. Role: {Role}. ErrorCount: {ErrorCount}.",
                user.Id,
                UserRoles.NormalUser,
                roleResult.Errors.Count());
            return CreateUserAccountResult.Failure(ToAuthErrors(roleResult.Errors));
        }

        logger.LogInformation("Identity user created. UserId: {UserId}. Role: {Role}.", user.Id, UserRoles.NormalUser);

        return CreateUserAccountResult.Success(new SignUpResponse(
            user.Id,
            user.Email!,
            user.FullName,
            UserRoles.NormalUser,
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

        logger.LogInformation("Credentials validation succeeded. UserId: {UserId}. RoleCount: {RoleCount}.", user.Id, roles.Count);

        return CredentialsValidationResult.Success(new AuthenticatedUser(
            user.Id,
            user.Email!,
            user.FullName,
            roles.ToArray()));
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
