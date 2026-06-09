using IdentityService.Api.Auth;

namespace IdentityService.Api.Middleware;

internal sealed class HostedSignInRequestValidationMiddleware(
    RequestDelegate next,
    ILogger<HostedSignInRequestValidationMiddleware> logger)
{
    public const string ErrorMessageItemKey = "HostedSignInRequestValidation.ErrorMessage";

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

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method) &&
            context.Request.Path.Equals(HostedSignInConstants.SignInPath, StringComparison.OrdinalIgnoreCase) &&
            !TryValidate(context.Request.Query, out var message))
        {
            logger.LogWarning("Hosted sign-in request validation failed before page handling.");
            context.Items[ErrorMessageItemKey] = message;
            context.Request.Path = "/auth/sign-in/request-error";
        }

        await next(context);
    }

    private static bool TryValidate(IQueryCollection query, out string message)
    {
        var missingParameters = RequiredAuthorizationParameters
            .Where(parameter => string.IsNullOrWhiteSpace(query[parameter]))
            .ToArray();

        if (missingParameters.Length > 0)
        {
            message = BuildInvalidRequestMessage(missingParameters);
            return false;
        }

        if (!string.Equals(query["response_type"], "code", StringComparison.Ordinal))
        {
            message = BuildInvalidRequestMessage(["response_type=code"]);
            return false;
        }

        if (!string.Equals(query["code_challenge_method"], "S256", StringComparison.OrdinalIgnoreCase))
        {
            message = BuildInvalidRequestMessage(["code_challenge_method=S256"]);
            return false;
        }

        message = string.Empty;
        return true;
    }

    private static string BuildInvalidRequestMessage(IEnumerable<string> missingOrInvalidItems)
    {
        return $"Missing or invalid authorization parameters: {string.Join(", ", missingOrInvalidItems)}.";
    }
}

internal static class HostedSignInRequestValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseHostedSignInRequestValidation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<HostedSignInRequestValidationMiddleware>();
    }
}
