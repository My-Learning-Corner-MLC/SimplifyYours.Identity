using IdentityService.Application;
using IdentityService.Contracts;
using MediatR;

namespace IdentityService.Application.SignUp;

public sealed class SignUpCommandHandler(
    IUserAccountService userAccountService,
    TimeProvider timeProvider) : IRequestHandler<SignUpCommand, SignUpResult>
{
    public async Task<SignUpResult> Handle(SignUpCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email!.Trim();
        var emailExists = await userAccountService.EmailExistsAsync(normalizedEmail, cancellationToken);

        if (emailExists)
        {
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

        return createResult.Succeeded
            ? SignUpResult.Success(createResult.User!)
            : SignUpResult.Failure(createResult.Errors);
    }
}
