using IdentityService.Application;
using IdentityService.Application.SignIn;
using IdentityService.Application.SignUp;
using IdentityService.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdentityService.UnitTests.SignIn;

public sealed class SignInCommandHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsUser_WhenCredentialsAreValid()
    {
        var authenticatedUser = new AuthenticatedUser(
            Guid.NewGuid(),
            "avery@example.com",
            "Avery Nguyen",
            Guid.NewGuid(),
            new[] { UserRoles.NormalUser },
            Permissions.All);
        var accountService = new FakeUserAccountService(
            CredentialsValidationResult.Success(authenticatedUser));
        var handler = new SignInCommandHandler(
            accountService,
            NullLogger<SignInCommandHandler>.Instance);

        var result = await handler.Handle(
            new SignInCommand(" avery@example.com ", "Password1!"),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(authenticatedUser, result.User);
        Assert.Equal("avery@example.com", accountService.ValidatedEmail);
    }

    [Fact]
    public async Task Handle_ReturnsGenericFailure_WhenCredentialsAreInvalid()
    {
        var accountService = new FakeUserAccountService(
            CredentialsValidationResult.Failure(SignInFailureReason.InvalidCredentials));
        var handler = new SignInCommandHandler(
            accountService,
            NullLogger<SignInCommandHandler>.Instance);

        var result = await handler.Handle(
            new SignInCommand("avery@example.com", "wrong-password"),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == nameof(SignInFailureReason.InvalidCredentials));
        Assert.All(result.Errors, error => Assert.Contains("email/password", error.Message));
    }

    private sealed class FakeUserAccountService(CredentialsValidationResult credentialsValidationResult)
        : IUserAccountService
    {
        public string? ValidatedEmail { get; private set; }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<CreateUserAccountResult> CreateNormalUserAsync(
            string fullName,
            string email,
            string password,
            DateTimeOffset termsAcceptedAt,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<CredentialsValidationResult> ValidateCredentialsAsync(
            string email,
            string password,
            CancellationToken cancellationToken)
        {
            ValidatedEmail = email;

            return Task.FromResult(credentialsValidationResult);
        }
    }
}
