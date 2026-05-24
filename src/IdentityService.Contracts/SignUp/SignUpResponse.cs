namespace IdentityService.Contracts.SignUp;

public sealed record SignUpResponse(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    string Status);
