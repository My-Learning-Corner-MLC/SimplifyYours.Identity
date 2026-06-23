using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Infrastructure.Persistence;

public sealed class IdentityServiceDbContext(DbContextOptions<IdentityServiceDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.UseOpenIddict();

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.FullName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(user => user.CreatedAt)
                .IsRequired();

            entity.Property(user => user.TenantId)
                .IsRequired();

            entity.HasIndex(user => user.TenantId)
                .HasDatabaseName("ix_users_tenant_id");
        });

        builder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(tenant => tenant.Id);

            entity.Property(tenant => tenant.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(tenant => tenant.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();
        });

        builder.Entity<UserPermission>(entity =>
        {
            entity.ToTable("user_permissions");
            entity.HasKey(permission => new { permission.UserId, permission.Permission });

            entity.Property(permission => permission.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(permission => permission.Permission)
                .HasColumnName("permission")
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(permission => permission.UserId)
                .HasDatabaseName("ix_user_permissions_user_id");
        });
    }
}
