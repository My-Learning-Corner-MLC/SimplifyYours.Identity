using Microsoft.AspNetCore.Identity;

namespace IdentityService.Infrastructure.Persistence;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    public bool IsDisabled { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? TermsAcceptedAt { get; set; }

    public Guid TenantId { get; set; }

    public string[] Permissions { get; set; } = [];
}
