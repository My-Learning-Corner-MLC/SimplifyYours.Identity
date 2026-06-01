using IdentityService.Application.SignIn;
using IdentityService.Application.SignUp;
using IdentityService.Contracts;
using IdentityService.Contracts.SignUp;
using IdentityService.Domain.Identity;

namespace IdentityService.UnitTests;

public sealed class AuthContractMappingTests
{
    [Fact]
    public void SignUpCommand_FromRequest_CopiesRequestValues()
    {
        var request = new SignUpRequest(
            "Avery Nguyen",
            "avery@example.com",
            "Password1!",
            "Password1!",
            true);

        var command = SignUpCommand.FromRequest(request);

        Assert.Equal(request.FullName, command.FullName);
        Assert.Equal(request.Email, command.Email);
        Assert.Equal(request.Password, command.Password);
        Assert.Equal(request.ConfirmPassword, command.ConfirmPassword);
        Assert.Equal(request.AcceptTermsAndPrivacy, command.AcceptTermsAndPrivacy);
    }

    [Fact]
    public void AuthResponseContracts_ExposeExpectedValues()
    {
        var userId = Guid.NewGuid();
        var signUpResponse = new SignUpResponse(
            userId,
            "avery@example.com",
            "Avery Nguyen",
            UserRoles.NormalUser,
            "Active");
        var error = new AuthError("Email", "Email is already registered.");
        var errorResponse = new AuthErrorResponse(new[] { error });
        var authenticatedUser = new AuthenticatedUser(
            userId,
            "avery@example.com",
            "Avery Nguyen",
            new[] { UserRoles.NormalUser });

        Assert.Equal(userId, signUpResponse.UserId);
        Assert.Equal("avery@example.com", signUpResponse.Email);
        Assert.Equal("Avery Nguyen", signUpResponse.FullName);
        Assert.Equal(error, Assert.Single(errorResponse.Errors));
        Assert.Equal(userId, authenticatedUser.UserId);
        Assert.Equal("avery@example.com", authenticatedUser.Email);
        Assert.Equal("Avery Nguyen", authenticatedUser.FullName);
    }

    [Fact]
    public void CreateUserAccountResult_Failure_ReturnsErrorsWithoutUser()
    {
        var error = new AuthError("Password", "Password is invalid.");

        var result = CreateUserAccountResult.Failure(new[] { error });

        Assert.False(result.Succeeded);
        Assert.Null(result.User);
        Assert.Equal(error, Assert.Single(result.Errors));
    }
}
