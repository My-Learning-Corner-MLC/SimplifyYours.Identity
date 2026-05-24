using IdentityService.Application;
using IdentityService.Application.SignIn;
using IdentityService.Application.SignUp;
using IdentityService.Contracts;
using IdentityService.Contracts.SignUp;
using IdentityService.Domain.Identity;
using IdentityService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Infrastructure.Identity;

public sealed class IdentityUserAccountService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager) : IUserAccountService
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
            return CreateUserAccountResult.Failure(ToAuthErrors(createResult.Errors));
        }

        var roleResult = await userManager.AddToRoleAsync(user, UserRoles.NormalUser);

        if (!roleResult.Succeeded)
        {
            return CreateUserAccountResult.Failure(ToAuthErrors(roleResult.Errors));
        }

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
            return CredentialsValidationResult.Failure(SignInFailureReason.InvalidCredentials);
        }

        if (user.IsDisabled)
        {
            return CredentialsValidationResult.Failure(SignInFailureReason.Disabled);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return CredentialsValidationResult.Failure(SignInFailureReason.LockedOut);
        }

        var passwordMatches = await userManager.CheckPasswordAsync(user, password);

        if (!passwordMatches)
        {
            if (userManager.SupportsUserLockout)
            {
                await userManager.AccessFailedAsync(user);
            }

            return CredentialsValidationResult.Failure(SignInFailureReason.InvalidCredentials);
        }

        if (userManager.SupportsUserLockout)
        {
            await userManager.ResetAccessFailedCountAsync(user);
        }

        var roles = await userManager.GetRolesAsync(user);

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
            throw new InvalidOperationException($"Unable to create required role '{roleName}'.");
        }
    }

    private static IReadOnlyCollection<AuthError> ToAuthErrors(IEnumerable<IdentityError> errors)
    {
        return errors
            .Select(error => new AuthError(error.Code, error.Description))
            .ToArray();
    }
}
