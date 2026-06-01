using IdentityService.Api.Middleware;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityService.Api.Pages.Auth;

public sealed class SignInRequestErrorModel : PageModel
{
    public string Message { get; private set; } = "The authorization request is invalid.";

    public void OnGet()
    {
        Response.StatusCode = StatusCodes.Status400BadRequest;

        if (HttpContext.Items[HostedSignInRequestValidationMiddleware.ErrorMessageItemKey] is string message)
        {
            Message = message;
        }
    }
}
