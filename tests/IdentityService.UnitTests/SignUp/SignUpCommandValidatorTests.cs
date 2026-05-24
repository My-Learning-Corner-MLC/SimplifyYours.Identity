using IdentityService.Application.SignUp;

namespace IdentityService.UnitTests.SignUp;

public sealed class SignUpCommandValidatorTests
{
    private readonly SignUpCommandValidator _validator = new();

    [Fact]
    public void Validate_ReturnsSuccess_WhenCommandIsValid()
    {
        var command = new SignUpCommand(
            "Avery Nguyen",
            "avery@example.com",
            "Password1!",
            "Password1!",
            true);

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a")]
    public void Validate_ReturnsFailure_WhenFullNameIsInvalid(string fullName)
    {
        var command = new SignUpCommand(
            fullName,
            "avery@example.com",
            "Password1!",
            "Password1!",
            true);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SignUpCommand.FullName));
    }

    [Fact]
    public void Validate_ReturnsFailure_WhenEmailIsInvalid()
    {
        var command = new SignUpCommand(
            "Avery Nguyen",
            "not-an-email",
            "Password1!",
            "Password1!",
            true);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SignUpCommand.Email));
    }

    [Fact]
    public void Validate_ReturnsFailure_WhenPasswordConfirmationDoesNotMatch()
    {
        var command = new SignUpCommand(
            "Avery Nguyen",
            "avery@example.com",
            "Password1!",
            "Different1!",
            true);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SignUpCommand.ConfirmPassword));
    }

    [Fact]
    public void Validate_ReturnsFailure_WhenTermsAreNotAccepted()
    {
        var command = new SignUpCommand(
            "Avery Nguyen",
            "avery@example.com",
            "Password1!",
            "Password1!",
            false);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SignUpCommand.AcceptTermsAndPrivacy));
    }
}
