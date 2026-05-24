namespace IdentityService.Contracts.SignIn;

public sealed record SignInRequest(
    string? Email,
    string? Password);
