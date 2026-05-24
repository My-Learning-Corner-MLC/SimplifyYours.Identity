namespace IdentityService.Contracts;

public sealed record AuthError(
    string Code,
    string Message);
