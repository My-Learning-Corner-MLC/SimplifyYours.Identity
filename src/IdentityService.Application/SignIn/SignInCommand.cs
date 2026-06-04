using MediatR;

namespace IdentityService.Application.SignIn;

public sealed record SignInCommand(
    string? Email,
    string? Password) : IRequest<SignInResult>;
