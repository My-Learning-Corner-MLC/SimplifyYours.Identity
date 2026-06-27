using IdentityService.Api.Middleware;
using IdentityService.Api.Pages.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;

namespace IdentityService.UnitTests.Api.Pages.Auth;

public sealed class SignInRequestErrorModelTests
{
    [Fact]
    public void OnGet_SetsBadRequest_AndKeepsDefaultMessage_WhenNoItem()
    {
        var model = CreateModel(out var context);

        model.OnGet();

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("The authorization request is invalid.", model.Message);
    }

    [Fact]
    public void OnGet_OverridesMessage_WhenItemPresent()
    {
        var model = CreateModel(out var context);
        context.Items[HostedSignInRequestValidationMiddleware.ErrorMessageItemKey] =
            "Missing or invalid authorization parameters: client_id.";

        model.OnGet();

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal(
            "Missing or invalid authorization parameters: client_id.",
            model.Message);
    }

    [Fact]
    public void OnGet_IgnoresNonStringItem_AndKeepsDefaultMessage()
    {
        var model = CreateModel(out var context);
        context.Items[HostedSignInRequestValidationMiddleware.ErrorMessageItemKey] = 42;

        model.OnGet();

        Assert.Equal("The authorization request is invalid.", model.Message);
    }

    private static SignInRequestErrorModel CreateModel(out HttpContext context)
    {
        context = new DefaultHttpContext();
        var model = new SignInRequestErrorModel
        {
            PageContext = new PageContext
            {
                HttpContext = context,
                RouteData = new RouteData(),
                ActionDescriptor = new CompiledPageActionDescriptor()
            }
        };
        return model;
    }
}
