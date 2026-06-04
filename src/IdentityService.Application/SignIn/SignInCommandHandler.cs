using IdentityService.Application;
using IdentityService.Contracts;
using MediatR;

namespace IdentityService.Application.SignIn;

public sealed class SignInCommandHandler(
    IUserAccountService userAccountService) : IRequestHandler<SignInCommand, SignInResult>
{
    public async Task<SignInResult> Handle(SignInCommand request, CancellationToken cancellationToken)
    {
        var credentialsResult = await userAccountService.ValidateCredentialsAsync(
            request.Email!.Trim(),
            request.Password!,
            cancellationToken);

        if (!credentialsResult.Succeeded)
        {
            return SignInResult.Failure(new[]
            {
                new AuthError(
                    credentialsResult.FailureReason?.ToString() ?? "InvalidCredentials",
                    "The email/password combination is invalid.")
            });
        }

        return SignInResult.Success(credentialsResult.User!);
    }
}
