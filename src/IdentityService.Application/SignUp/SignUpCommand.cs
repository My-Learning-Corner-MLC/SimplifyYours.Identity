using IdentityService.Contracts.SignUp;
using MediatR;

namespace IdentityService.Application.SignUp;

public sealed record SignUpCommand(
    string? FullName,
    string? Email,
    string? Password,
    string? ConfirmPassword,
    bool AcceptTermsAndPrivacy) : IRequest<SignUpResult>
{
    public static SignUpCommand FromRequest(SignUpRequest request)
    {
        return new SignUpCommand(
            request.FullName,
            request.Email,
            request.Password,
            request.ConfirmPassword,
            request.AcceptTermsAndPrivacy);
    }
}
