using FluentValidation;
using IdentityService.Api.Auth;
using IdentityService.Application.SignIn;
using IdentityService.Contracts;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Server.AspNetCore;

namespace IdentityService.Api.Pages.Auth;

public sealed class SignInModel(
    ISender sender,
    ILogger<SignInModel> logger) : PageModel
{
    private static readonly string[] AuthorizationParameterNames =
    [
        "client_id",
        "redirect_uri",
        "response_type",
        "response_mode",
        "scope",
        "state",
        "nonce",
        "code_challenge",
        "code_challenge_method"
    ];

    private static readonly string[] RequiredAuthorizationParameters =
    [
        "client_id",
        "redirect_uri",
        "response_type",
        "scope",
        "state",
        "nonce",
        "code_challenge",
        "code_challenge_method"
    ];

    public string FormAction { get; private set; } = HostedSignInConstants.SignInPath;
    public IReadOnlyDictionary<string, string> AuthorizationParameters { get; private set; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
    public bool ShowInvalidCredentialsError { get; private set; }
    public bool ShowInvalidAuthorizationRequestError { get; private set; }
    public string InvalidAuthorizationRequestErrorMessage { get; private set; } = string.Empty;

    public string EmailFieldName => HostedSignInConstants.EmailFieldName;
    public string PasswordFieldName => HostedSignInConstants.PasswordFieldName;

    public IActionResult OnGet()
    {
        FormAction = HostedSignInConstants.SignInPath;
        AuthorizationParameters = GetAuthorizationParametersFromQuery();

        if (!TryValidateAuthorizationParameters(AuthorizationParameters, out var invalidRequestMessage))
        {
            logger.LogWarning("Hosted sign-in GET rejected because authorization parameters were invalid.");
            return RenderInvalidAuthorizationRequest(invalidRequestMessage);
        }

        if (HttpContext.GetOpenIddictServerRequest() is null)
        {
            logger.LogWarning("Hosted sign-in GET rejected because OpenIddict request was unavailable.");
            return RenderInvalidAuthorizationRequest("The authorization request could not be validated.");
        }

        ShowInvalidCredentialsError = string.Equals(
            Request.Query["error"],
            "invalid_credentials",
            StringComparison.Ordinal);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        FormAction = HostedSignInConstants.SignInPath;
        AuthorizationParameters = GetAuthorizationParametersFromForm();

        if (!TryValidateAuthorizationParameters(AuthorizationParameters, out var invalidRequestMessage))
        {
            logger.LogWarning("Hosted sign-in POST rejected because authorization parameters were invalid.");
            return RenderInvalidAuthorizationRequest(invalidRequestMessage);
        }

        var openIddictRequest = HttpContext.GetOpenIddictServerRequest();

        if (openIddictRequest is null)
        {
            logger.LogWarning("Hosted sign-in POST rejected because OpenIddict request was unavailable.");
            return RenderInvalidAuthorizationRequest("The authorization request could not be validated.");
        }

        var result = await ValidateCredentialsAsync(
            Request.Form[EmailFieldName].ToString(),
            Request.Form[PasswordFieldName].ToString(),
            cancellationToken);

        if (!result.Succeeded)
        {
            logger.LogWarning("Hosted sign-in failed. FailureCount: {FailureCount}.", result.Errors.Count);
            return RedirectToPage("/Auth/SignIn", BuildInvalidCredentialsRedirectValues());
        }

        logger.LogInformation("Hosted sign-in succeeded. UserId: {UserId}.", result.User!.UserId);

        var principal = OpenIddictClaimsPrincipalFactory.Create(result.User!, openIddictRequest);

        return SignIn(
            principal,
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static bool TryValidateAuthorizationParameters(
        IReadOnlyDictionary<string, string> parameters,
        out string message)
    {
        var missingParameters = RequiredAuthorizationParameters
            .Where(parameter => !parameters.TryGetValue(parameter, out var value) || string.IsNullOrWhiteSpace(value))
            .ToArray();

        if (missingParameters.Length > 0)
        {
            message = BuildInvalidRequestMessage(missingParameters);
            return false;
        }

        if (!string.Equals(parameters["response_type"], "code", StringComparison.Ordinal))
        {
            message = BuildInvalidRequestMessage(["response_type=code"]);
            return false;
        }

        if (!string.Equals(parameters["code_challenge_method"], "S256", StringComparison.OrdinalIgnoreCase))
        {
            message = BuildInvalidRequestMessage(["code_challenge_method=S256"]);
            return false;
        }

        message = string.Empty;
        return true;
    }

    private IActionResult RenderInvalidAuthorizationRequest(string message)
    {
        Response.StatusCode = StatusCodes.Status400BadRequest;
        ShowInvalidAuthorizationRequestError = true;
        InvalidAuthorizationRequestErrorMessage = message;
        return Page();
    }

    private RouteValueDictionary BuildInvalidCredentialsRedirectValues()
    {
        var values = new RouteValueDictionary(
            AuthorizationParameters.ToDictionary(
                pair => pair.Key,
                pair => (object?)pair.Value,
                StringComparer.Ordinal));

        values["error"] = "invalid_credentials";

        return values;
    }

    private Dictionary<string, string> GetAuthorizationParametersFromQuery()
    {
        return AuthorizationParameterNames
            .Where(parameter => Request.Query.ContainsKey(parameter))
            .ToDictionary(
                parameter => parameter,
                parameter => Request.Query[parameter].ToString(),
                StringComparer.Ordinal);
    }

    private Dictionary<string, string> GetAuthorizationParametersFromForm()
    {
        return AuthorizationParameterNames
            .Where(parameter => Request.Form.ContainsKey(parameter))
            .ToDictionary(
                parameter => parameter,
                parameter => Request.Form[parameter].ToString(),
                StringComparer.Ordinal);
    }

    private static string BuildInvalidRequestMessage(IEnumerable<string> missingOrInvalidItems)
    {
        return $"Missing or invalid authorization parameters: {string.Join(", ", missingOrInvalidItems)}.";
    }

    private async Task<Application.SignIn.SignInResult> ValidateCredentialsAsync(
        string? email,
        string? password,
        CancellationToken cancellationToken)
    {
        try
        {
            return await sender.Send(new SignInCommand(email, password), cancellationToken);
        }
        catch (ValidationException)
        {
            return Application.SignIn.SignInResult.Failure([
                new AuthError("InvalidCredentials", "The email/password combination is invalid.")
            ]);
        }
    }
}
