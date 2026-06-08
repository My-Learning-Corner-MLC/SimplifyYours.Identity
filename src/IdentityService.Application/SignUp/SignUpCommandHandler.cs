using IdentityService.Application;
using IdentityService.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentityService.Application.SignUp;

public sealed class SignUpCommandHandler(
    IUserAccountService userAccountService,
    TimeProvider timeProvider,
    ILogger<SignUpCommandHandler> logger) : IRequestHandler<SignUpCommand, SignUpResult>
{
    public async Task<SignUpResult> Handle(SignUpCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email!.Trim();
        var emailExists = await userAccountService.EmailExistsAsync(normalizedEmail, cancellationToken);

        if (emailExists)
        {
            logger.LogWarning("Sign-up rejected because email already exists.");
            return SignUpResult.Failure(new[]
            {
                new AuthError("Email", "An account with this email address already exists.")
            });
        }

        var createResult = await userAccountService.CreateNormalUserAsync(
            request.FullName!.Trim(),
            normalizedEmail,
            request.Password!,
            timeProvider.GetUtcNow(),
            cancellationToken);

        if (!createResult.Succeeded)
        {
            logger.LogWarning(
                "Sign-up user creation failed. ErrorCount: {ErrorCount}.",
                createResult.Errors.Count);
            return SignUpResult.Failure(createResult.Errors);
        }

        logger.LogInformation("Sign-up user account created. UserId: {UserId}.", createResult.User!.UserId);

        return SignUpResult.Success(createResult.User);
    }
}
