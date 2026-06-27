using IdentityService.Application.SignIn;
using IdentityService.Domain.Identity;
using IdentityService.Infrastructure.Identity;
using IdentityService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IdentityService.UnitTests.Infrastructure.Identity;

public sealed class IdentityUserAccountServiceTests
{
    [Fact]
    public async Task EmailExistsAsync_ReturnsTrue_WhenUserFound()
    {
        var (service, userManager, _) = CreateService();
        userManager.Setup(u => u.FindByEmailAsync("avery@example.com"))
            .ReturnsAsync(new ApplicationUser { Email = "avery@example.com" });

        var result = await service.EmailExistsAsync("avery@example.com", CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task EmailExistsAsync_ReturnsFalse_WhenUserNotFound()
    {
        var (service, userManager, _) = CreateService();
        userManager.Setup(u => u.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await service.EmailExistsAsync("missing@example.com", CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task EmailExistsAsync_ThrowsWhenCancelled()
    {
        var (service, _, _) = CreateService();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.EmailExistsAsync("a@b.c", cts.Token));
    }

    [Fact]
    public async Task CreateTenantAdminAsync_ReturnsSuccess_AndAssignsRole()
    {
        var (service, userManager, roleManager) = CreateService();
        roleManager.Setup(r => r.RoleExistsAsync(UserRoles.TenantAdmin)).ReturnsAsync(true);
        userManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), UserRoles.TenantAdmin))
            .ReturnsAsync(IdentityResult.Success);

        var result = await service.CreateTenantAdminAsync(
            "Avery Nguyen",
            "avery@example.com",
            "Password1!",
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.User);
        Assert.Equal("avery@example.com", result.User!.Email);
        Assert.Equal(UserRoles.TenantAdmin, result.User.Role);
    }

    [Fact]
    public async Task CreateTenantAdminAsync_CreatesRole_WhenMissing()
    {
        var (service, userManager, roleManager) = CreateService();
        roleManager.Setup(r => r.RoleExistsAsync(UserRoles.TenantAdmin)).ReturnsAsync(false);
        roleManager.Setup(r => r.CreateAsync(It.IsAny<IdentityRole<Guid>>()))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), UserRoles.TenantAdmin))
            .ReturnsAsync(IdentityResult.Success);

        var result = await service.CreateTenantAdminAsync(
            "Avery", "a@b.c", "Password1!", DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.True(result.Succeeded);
        roleManager.Verify(r => r.CreateAsync(It.Is<IdentityRole<Guid>>(role => role.Name == UserRoles.TenantAdmin)), Times.Once);
    }

    [Fact]
    public async Task CreateTenantAdminAsync_Throws_WhenRoleCreationFails()
    {
        var (service, _, roleManager) = CreateService();
        roleManager.Setup(r => r.RoleExistsAsync(UserRoles.TenantAdmin)).ReturnsAsync(false);
        roleManager.Setup(r => r.CreateAsync(It.IsAny<IdentityRole<Guid>>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "X", Description = "nope" }));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateTenantAdminAsync(
                "Avery", "a@b.c", "Password1!", DateTimeOffset.UtcNow, CancellationToken.None));
    }

    [Fact]
    public async Task CreateTenantAdminAsync_ReturnsFailure_WhenUserCreationFails()
    {
        var (service, userManager, roleManager) = CreateService();
        roleManager.Setup(r => r.RoleExistsAsync(UserRoles.TenantAdmin)).ReturnsAsync(true);
        userManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "Password", Description = "Too short." }));

        var result = await service.CreateTenantAdminAsync(
            "Avery", "a@b.c", "x", DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.False(result.Succeeded);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Password", error.Code);
        Assert.Equal("Too short.", error.Message);
    }

    [Fact]
    public async Task CreateTenantAdminAsync_ReturnsFailure_WhenRoleAssignmentFails()
    {
        var (service, userManager, roleManager) = CreateService();
        roleManager.Setup(r => r.RoleExistsAsync(UserRoles.TenantAdmin)).ReturnsAsync(true);
        userManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), UserRoles.TenantAdmin))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "Role", Description = "nope" }));

        var result = await service.CreateTenantAdminAsync(
            "Avery", "a@b.c", "Password1!", DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.False(result.Succeeded);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Role", error.Code);
    }

    [Fact]
    public async Task CreateTenantAdminAsync_ThrowsWhenCancelled()
    {
        var (service, _, _) = CreateService();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.CreateTenantAdminAsync(
                "Avery", "a@b.c", "p", DateTimeOffset.UtcNow, cts.Token));
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ReturnsInvalidCredentials_WhenUserMissing()
    {
        var (service, userManager, _) = CreateService();
        userManager.Setup(u => u.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await service.ValidateCredentialsAsync("a@b.c", "x", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(SignInFailureReason.InvalidCredentials, result.FailureReason);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ReturnsDisabled_WhenUserDisabled()
    {
        var (service, userManager, _) = CreateService();
        userManager.Setup(u => u.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new ApplicationUser { Id = Guid.NewGuid(), IsDisabled = true });

        var result = await service.ValidateCredentialsAsync("a@b.c", "x", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(SignInFailureReason.Disabled, result.FailureReason);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ReturnsLockedOut_WhenLocked()
    {
        var (service, userManager, _) = CreateService();
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "a@b.c" };
        userManager.Setup(u => u.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        userManager.Setup(u => u.IsLockedOutAsync(user)).ReturnsAsync(true);

        var result = await service.ValidateCredentialsAsync("a@b.c", "x", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(SignInFailureReason.LockedOut, result.FailureReason);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ReturnsInvalid_WhenPasswordWrong_AndIncrementsFailureCount()
    {
        var (service, userManager, _) = CreateService();
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "a@b.c", FullName = "Avery" };
        userManager.Setup(u => u.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        userManager.Setup(u => u.IsLockedOutAsync(user)).ReturnsAsync(false);
        userManager.Setup(u => u.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(false);
        userManager.SetupGet(u => u.SupportsUserLockout).Returns(true);
        userManager.Setup(u => u.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await service.ValidateCredentialsAsync("a@b.c", "wrong", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(SignInFailureReason.InvalidCredentials, result.FailureReason);
        userManager.Verify(u => u.AccessFailedAsync(user), Times.Once);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ReturnsSuccess_WhenPasswordCorrect_AndResetsFailureCount()
    {
        var (service, userManager, _) = CreateService();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "avery@example.com",
            FullName = "Avery Nguyen"
        };
        userManager.Setup(u => u.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        userManager.Setup(u => u.IsLockedOutAsync(user)).ReturnsAsync(false);
        userManager.Setup(u => u.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(true);
        userManager.SetupGet(u => u.SupportsUserLockout).Returns(true);
        userManager.Setup(u => u.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { UserRoles.TenantAdmin });

        var result = await service.ValidateCredentialsAsync("avery@example.com", "p", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.User);
        Assert.Equal("avery@example.com", result.User!.Email);
        Assert.Equal("Avery Nguyen", result.User.FullName);
        Assert.Contains(UserRoles.TenantAdmin, result.User.Roles);
        userManager.Verify(u => u.ResetAccessFailedCountAsync(user), Times.Once);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_DoesNotTouchLockoutCounters_WhenLockoutUnsupported()
    {
        var (service, userManager, _) = CreateService();
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "a@b.c", FullName = "A" };
        userManager.Setup(u => u.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        userManager.Setup(u => u.IsLockedOutAsync(user)).ReturnsAsync(false);
        userManager.Setup(u => u.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(false);
        userManager.SetupGet(u => u.SupportsUserLockout).Returns(false);

        var result = await service.ValidateCredentialsAsync("a@b.c", "x", CancellationToken.None);

        Assert.False(result.Succeeded);
        userManager.Verify(u => u.AccessFailedAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ThrowsWhenCancelled()
    {
        var (service, _, _) = CreateService();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.ValidateCredentialsAsync("a@b.c", "x", cts.Token));
    }

    private static (IdentityUserAccountService Service,
                    Mock<UserManager<ApplicationUser>> UserManager,
                    Mock<RoleManager<IdentityRole<Guid>>> RoleManager) CreateService()
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var userManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var roleStore = new Mock<IRoleStore<IdentityRole<Guid>>>();
        var roleManager = new Mock<RoleManager<IdentityRole<Guid>>>(
            roleStore.Object, null!, null!, null!, null!);

        var options = new DbContextOptionsBuilder<IdentityServiceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var dbContext = new IdentityServiceDbContext(options);

        var service = new IdentityUserAccountService(
            userManager.Object,
            roleManager.Object,
            dbContext,
            TimeProvider.System,
            NullLogger<IdentityUserAccountService>.Instance);

        return (service, userManager, roleManager);
    }
}
