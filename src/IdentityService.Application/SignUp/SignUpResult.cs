using IdentityService.Contracts;
using IdentityService.Contracts.SignUp;

namespace IdentityService.Application.SignUp;

public sealed record SignUpResult(
    bool Succeeded,
    SignUpResponse? User,
    IReadOnlyCollection<AuthError> Errors)
{
    public static SignUpResult Success(SignUpResponse user)
    {
        return new SignUpResult(true, user, Array.Empty<AuthError>());
    }

    public static SignUpResult Failure(IReadOnlyCollection<AuthError> errors)
    {
        return new SignUpResult(false, null, errors);
    }
}
