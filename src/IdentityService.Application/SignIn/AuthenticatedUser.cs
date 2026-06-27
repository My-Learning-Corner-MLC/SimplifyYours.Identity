namespace IdentityService.Application.SignIn;

public sealed record AuthenticatedUser(
    Guid UserId,
    string Email,
    string FullName,
    Guid TenantId,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
