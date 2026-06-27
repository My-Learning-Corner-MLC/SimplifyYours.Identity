using System.Security.Claims;
using IdentityService.Api.Pages.Auth;
using IdentityService.Application.SignIn;
using IdentityService.Contracts;
using IdentityService.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Moq;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;

namespace IdentityService.UnitTests.SignIn;

public sealed class SignInModelOnGetTests
{
    [Fact]
    public void OnGet_RendersPage_WhenAuthorizationRequestValid()
    {
        var model = CreateModel(ValidQuery(), withOpenIddictTransaction: true);

        var result = model.OnGet();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ShowInvalidAuthorizationRequestError);
    }

    [Fact]
    public void OnGet_RendersPageWithError_WhenRequiredQueryMissing()
    {
        var query = ValidQuery();
        query.Remove("client_id");

        var model = CreateModel(query, withOpenIddictTransaction: true);

        var result = model.OnGet();

        Assert.IsType<PageResult>(result);
        Assert.True(model.ShowInvalidAuthorizationRequestError);
        Assert.Contains("client_id", model.InvalidAuthorizationRequestErrorMessage);
        Assert.Equal(
            StatusCodes.Status400BadRequest,
            ((DefaultHttpContext)model.PageContext.HttpContext).Response.StatusCode);
    }

    [Fact]
    public void OnGet_RendersPageWithError_WhenResponseTypeNotCode()
    {
        var query = ValidQuery();
        query["response_type"] = "token";

        var model = CreateModel(query, withOpenIddictTransaction: true);

        model.OnGet();

        Assert.True(model.ShowInvalidAuthorizationRequestError);
        Assert.Contains("response_type=code", model.InvalidAuthorizationRequestErrorMessage);
    }

    [Fact]
    public void OnGet_RendersPageWithError_WhenChallengeMethodNotS256()
    {
        var query = ValidQuery();
        query["code_challenge_method"] = "plain";

        var model = CreateModel(query, withOpenIddictTransaction: true);

        model.OnGet();

        Assert.True(model.ShowInvalidAuthorizationRequestError);
        Assert.Contains("code_challenge_method=S256", model.InvalidAuthorizationRequestErrorMessage);
    }

    [Fact]
    public void OnGet_RendersPageWithError_WhenOpenIddictFeatureMissing()
    {
        var model = CreateModel(ValidQuery(), withOpenIddictTransaction: false);

        model.OnGet();

        Assert.True(model.ShowInvalidAuthorizationRequestError);
        Assert.Equal(
            "The authorization request could not be validated.",
            model.InvalidAuthorizationRequestErrorMessage);
    }

    [Fact]
    public void OnGet_SetsShowInvalidCredentialsError_WhenErrorQueryPresent()
    {
        var query = ValidQuery();
        query["error"] = "invalid_credentials";

        var model = CreateModel(query, withOpenIddictTransaction: true);

        model.OnGet();

        Assert.True(model.ShowInvalidCredentialsError);
    }

    [Fact]
    public void OnGet_ParsesRememberMeFromQuery()
    {
        var query = ValidQuery();
        query["remember_me"] = "true";

        var model = CreateModel(query, withOpenIddictTransaction: true);

        model.OnGet();

        Assert.True(model.RememberMe);
    }

    [Fact]
    public async Task OnPostAsync_RendersPageWithError_WhenRequiredFormParamMissing()
    {
        var form = ValidQuery();
        form.Remove("client_id");

        var model = CreateModel(query: null, withOpenIddictTransaction: true, formOverride: form);

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.True(model.ShowInvalidAuthorizationRequestError);
    }

    [Fact]
    public async Task OnPostAsync_RendersPageWithError_WhenOpenIddictFeatureMissing()
    {
        var model = CreateModel(query: null, withOpenIddictTransaction: false, formOverride: ValidQuery());

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.True(model.ShowInvalidAuthorizationRequestError);
    }

    private static SignInModel CreateModel(
        Dictionary<string, StringValues>? query,
        bool withOpenIddictTransaction,
        Dictionary<string, StringValues>? formOverride = null)
    {
        var sender = new Mock<ISender>();
        var authService = new Mock<IAuthenticationService>();
        authService.Setup(s => s.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(authService.Object);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        if (formOverride is not null)
        {
            httpContext.Request.Method = HttpMethods.Post;
            httpContext.Request.Form = new FormCollection(formOverride);
        }
        else
        {
            httpContext.Request.Method = HttpMethods.Get;
            if (query is not null)
            {
                httpContext.Request.QueryString = QueryString.Create(query.SelectMany(kv =>
                    kv.Value.Select(v => new KeyValuePair<string, string?>(kv.Key, v))));
            }
        }

        if (withOpenIddictTransaction)
        {
            var transaction = new OpenIddictServerTransaction { Request = new OpenIddictRequest() };
            httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature { Transaction = transaction });
        }

        return new SignInModel(sender.Object, NullLogger<SignInModel>.Instance)
        {
            PageContext = new PageContext
            {
                HttpContext = httpContext,
                RouteData = new RouteData(),
                ActionDescriptor = new CompiledPageActionDescriptor()
            }
        };
    }

    private static Dictionary<string, StringValues> ValidQuery() => new()
    {
        ["client_id"] = "spa-client",
        ["redirect_uri"] = "https://app.example.com/callback",
        ["response_type"] = "code",
        ["scope"] = "openid profile email",
        ["state"] = "state-xyz",
        ["nonce"] = "nonce-abc",
        ["code_challenge"] = "challenge",
        ["code_challenge_method"] = "S256"
    };
}
