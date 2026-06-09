using IdentityService.Application;
using IdentityService.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentityService.Application.SignIn;

public sealed class SignInCommandHandler(
    IUserAccountService userAccountService,
    ILogger<SignInCommandHandler> logger) : IRequestHandler<SignInCommand, SignInResult>
{
    public async Task<SignInResult> Handle(SignInCommand request, CancellationToken cancellationToken)
    {
        var credentialsResult = await userAccountService.ValidateCredentialsAsync(
            request.Email!.Trim(),
            request.Password!,
            cancellationToken);

        if (!credentialsResult.Succeeded)
        {
            logger.LogWarning(
                "Sign-in rejected. FailureReason: {FailureReason}.",
                credentialsResult.FailureReason?.ToString() ?? "Unknown");
            return SignInResult.Failure(new[]
            {
                new AuthError(
                    credentialsResult.FailureReason?.ToString() ?? "InvalidCredentials",
                    "The email/password combination is invalid.")
            });
        }

        logger.LogInformation("Sign-in credentials accepted. UserId: {UserId}.", credentialsResult.User!.UserId);

        return SignInResult.Success(credentialsResult.User!);
    }
}
