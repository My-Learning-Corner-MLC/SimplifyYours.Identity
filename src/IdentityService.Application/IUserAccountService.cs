using IdentityService.Application.SignIn;
using IdentityService.Application.SignUp;

namespace IdentityService.Application;

public interface IUserAccountService
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    Task<CreateUserAccountResult> CreateTenantAdminAsync(
        string fullName,
        string email,
        string password,
        DateTimeOffset termsAcceptedAt,
        CancellationToken cancellationToken);

    Task<CredentialsValidationResult> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken);
}
