using FluentValidation;
using IdentityService.Application.SignIn;
using IdentityService.Application.Common.Validation;
using IdentityService.Domain.Identity;

namespace IdentityService.UnitTests.Common.Validation;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_CallsNext_WhenNoValidatorsExist()
    {
        var authenticatedUser = new AuthenticatedUser(
            Guid.NewGuid(),
            "avery@example.com",
            "Avery Nguyen",
            Guid.NewGuid(),
            new[] { UserRoles.NormalUser },
            Permissions.All);
        var expectedResult = SignInResult.Success(authenticatedUser);
        var behavior = new ValidationBehavior<SignInCommand, SignInResult>(
            Array.Empty<IValidator<SignInCommand>>());
        var nextWasCalled = false;

        var result = await behavior.Handle(
            new SignInCommand("avery@example.com", "Password1!"),
            () =>
            {
                nextWasCalled = true;
                return Task.FromResult(expectedResult);
            },
            CancellationToken.None);

        Assert.True(nextWasCalled);
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task Handle_CallsNext_WhenValidatorsPass()
    {
        var authenticatedUser = new AuthenticatedUser(
            Guid.NewGuid(),
            "avery@example.com",
            "Avery Nguyen",
            Guid.NewGuid(),
            new[] { UserRoles.NormalUser },
            Permissions.All);
        var expectedResult = SignInResult.Success(authenticatedUser);
        var behavior = new ValidationBehavior<SignInCommand, SignInResult>(
            new[] { new SignInCommandValidator() });

        var result = await behavior.Handle(
            new SignInCommand("avery@example.com", "Password1!"),
            () => Task.FromResult(expectedResult),
            CancellationToken.None);

        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task Handle_ThrowsValidationException_WhenValidatorsFail()
    {
        var behavior = new ValidationBehavior<SignInCommand, SignInResult>(
            new[] { new SignInCommandValidator() });

        var exception = await Assert.ThrowsAsync<ValidationException>(() => behavior.Handle(
            new SignInCommand("not-an-email", ""),
            () => Task.FromResult(SignInResult.Failure(Array.Empty<IdentityService.Contracts.AuthError>())),
            CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(SignInCommand.Email));
        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(SignInCommand.Password));
    }
}
