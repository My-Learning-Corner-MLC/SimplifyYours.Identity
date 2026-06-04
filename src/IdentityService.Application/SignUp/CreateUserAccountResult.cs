using IdentityService.Contracts;
using IdentityService.Contracts.SignUp;

namespace IdentityService.Application.SignUp;

public sealed record CreateUserAccountResult(
    bool Succeeded,
    SignUpResponse? User,
    IReadOnlyCollection<AuthError> Errors)
{
    public static CreateUserAccountResult Success(SignUpResponse user)
    {
        return new CreateUserAccountResult(true, user, Array.Empty<AuthError>());
    }

    public static CreateUserAccountResult Failure(IReadOnlyCollection<AuthError> errors)
    {
        return new CreateUserAccountResult(false, null, errors);
    }
}
