namespace IdentityService.Contracts;

public sealed record AuthErrorResponse(
    IReadOnlyCollection<AuthError> Errors);
