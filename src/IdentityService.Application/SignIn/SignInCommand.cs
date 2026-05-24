using IdentityService.Contracts.SignIn;
using MediatR;

namespace IdentityService.Application.SignIn;

public sealed record SignInCommand(
    string? Email,
    string? Password) : IRequest<SignInResult>
{
    public static SignInCommand FromRequest(SignInRequest request)
    {
        return new SignInCommand(request.Email, request.Password);
    }
}
