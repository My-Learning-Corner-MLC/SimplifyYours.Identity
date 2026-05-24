using IdentityService.Application.SignIn;

namespace IdentityService.UnitTests.SignIn;

public sealed class SignInCommandValidatorTests
{
    private readonly SignInCommandValidator _validator = new();

    [Fact]
    public void Validate_ReturnsSuccess_WhenCommandIsValid()
    {
        var command = new SignInCommand("avery@example.com", "Password1!");

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ReturnsFailure_WhenEmailIsMissing()
    {
        var command = new SignInCommand("", "Password1!");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SignInCommand.Email));
    }

    [Fact]
    public void Validate_ReturnsFailure_WhenPasswordIsMissing()
    {
        var command = new SignInCommand("avery@example.com", "");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SignInCommand.Password));
    }
}
