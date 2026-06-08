using IdentityService.Application;
using IdentityService.Application.SignIn;
using IdentityService.Application.SignUp;
using IdentityService.Contracts;
using IdentityService.Contracts.SignUp;
using IdentityService.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdentityService.UnitTests.SignUp;

public sealed class SignUpCommandHandlerTests
{
    private static readonly DateTimeOffset FixedUtcNow = new(2026, 5, 17, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_CreatesNormalUser_WhenCommandIsValid()
    {
        var accountService = new FakeUserAccountService();
        var handler = CreateHandler(accountService);
        var command = new SignUpCommand(
            " Avery Nguyen ",
            " avery@example.com ",
            "Password1!",
            "Password1!",
            true);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.User);
        Assert.Equal("avery@example.com", accountService.CreatedEmail);
        Assert.Equal("Avery Nguyen", accountService.CreatedFullName);
        Assert.Equal(UserRoles.NormalUser, result.User.Role);
        Assert.Equal(FixedUtcNow, accountService.TermsAcceptedAt);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenEmailAlreadyExists()
    {
        var accountService = new FakeUserAccountService { EmailExists = true };
        var handler = CreateHandler(accountService);
        var command = new SignUpCommand(
            "Avery Nguyen",
            "avery@example.com",
            "Password1!",
            "Password1!",
            true);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Null(result.User);
        Assert.Contains(result.Errors, error => error.Code == "Email");
        Assert.False(accountService.CreateWasCalled);
    }

    private static SignUpCommandHandler CreateHandler(FakeUserAccountService accountService)
    {
        return new SignUpCommandHandler(
            accountService,
            new FixedTimeProvider(FixedUtcNow),
            NullLogger<SignUpCommandHandler>.Instance);
    }

    private sealed class FakeUserAccountService : IUserAccountService
    {
        public bool EmailExists { get; init; }

        public bool CreateWasCalled { get; private set; }

        public string? CreatedFullName { get; private set; }

        public string? CreatedEmail { get; private set; }

        public DateTimeOffset TermsAcceptedAt { get; private set; }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
        {
            return Task.FromResult(EmailExists);
        }

        public Task<CreateUserAccountResult> CreateNormalUserAsync(
            string fullName,
            string email,
            string password,
            DateTimeOffset termsAcceptedAt,
            CancellationToken cancellationToken)
        {
            CreateWasCalled = true;
            CreatedFullName = fullName;
            CreatedEmail = email;
            TermsAcceptedAt = termsAcceptedAt;

            return Task.FromResult(CreateUserAccountResult.Success(new SignUpResponse(
                Guid.NewGuid(),
                email,
                fullName,
                UserRoles.NormalUser,
                "Active")));
        }

        public Task<CredentialsValidationResult> ValidateCredentialsAsync(
            string email,
            string password,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
