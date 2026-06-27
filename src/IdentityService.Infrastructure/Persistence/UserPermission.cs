namespace IdentityService.Infrastructure.Persistence;

public sealed class UserPermission
{
    public Guid UserId { get; set; }

    public string Permission { get; set; } = string.Empty;
}
