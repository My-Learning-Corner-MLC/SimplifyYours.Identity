namespace IdentityService.Application.SignIn;

public sealed record AuthenticatedUser(
    Guid UserId,
    string Email,
    string FullName,
    IReadOnlyCollection<string> Roles);
