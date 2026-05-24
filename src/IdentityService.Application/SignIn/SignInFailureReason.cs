namespace IdentityService.Application.SignIn;

public enum SignInFailureReason
{
    InvalidCredentials = 0,
    LockedOut = 1,
    Disabled = 2
}
