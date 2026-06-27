namespace IdentityService.Infrastructure.Persistence;

public sealed class Tenant
{
    public Guid Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
