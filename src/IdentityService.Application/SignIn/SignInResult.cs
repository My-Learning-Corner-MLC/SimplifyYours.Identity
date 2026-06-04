using IdentityService.Contracts;

namespace IdentityService.Application.SignIn;

public sealed record SignInResult(
    bool Succeeded,
    AuthenticatedUser? User,
    IReadOnlyCollection<AuthError> Errors)
{
    public static SignInResult Success(AuthenticatedUser user)
    {
        return new SignInResult(true, user, Array.Empty<AuthError>());
    }

    public static SignInResult Failure(IReadOnlyCollection<AuthError> errors)
    {
        return new SignInResult(false, null, errors);
    }
}
