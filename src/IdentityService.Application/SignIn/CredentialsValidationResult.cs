namespace IdentityService.Application.SignIn;

public sealed record CredentialsValidationResult(
    bool Succeeded,
    AuthenticatedUser? User,
    SignInFailureReason? FailureReason)
{
    public static CredentialsValidationResult Success(AuthenticatedUser user)
    {
        return new CredentialsValidationResult(true, user, null);
    }

    public static CredentialsValidationResult Failure(SignInFailureReason failureReason)
    {
        return new CredentialsValidationResult(false, null, failureReason);
    }
}
