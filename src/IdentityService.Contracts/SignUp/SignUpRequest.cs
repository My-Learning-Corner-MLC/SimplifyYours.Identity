namespace IdentityService.Contracts.SignUp;

public sealed record SignUpRequest(
    string? FullName,
    string? Email,
    string? Password,
    string? ConfirmPassword,
    bool AcceptTermsAndPrivacy);
