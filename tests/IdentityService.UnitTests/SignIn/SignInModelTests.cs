using System.Security.Claims;
using IdentityService.Api.Pages.Auth;
using IdentityService.Application;
using IdentityService.Application.SignIn;
using IdentityService.Contracts;
using IdentityService.Domain.Identity;
using IdentityService.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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

public sealed class SignInModelTests
{
    [Fact]
    public async Task OnPostAsync_WhenRememberMeChecked_SignsIntoCookieSchemeWithPersistentExpiry()
    {
        var harness = CreateHarness(
            signInResult: SuccessfulSignIn(),
            formOverrides: new Dictionary<string, StringValues> { ["remember_me"] = "true" });

        var before = DateTimeOffset.UtcNow;
        var result = await harness.Model.OnPostAsync(CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        Assert.IsType<Microsoft.AspNetCore.Mvc.SignInResult>(result);
        Assert.True(harness.Model.RememberMe);
        harness.AuthenticationService.Verify(
            s => s.SignInAsync(
                It.IsAny<HttpContext>(),
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.IsAny<ClaimsPrincipal>(),
                It.Is<AuthenticationProperties>(p =>
                    p.IsPersistent
                    && p.AllowRefresh == true
                    && p.ExpiresUtc.HasValue
                    && p.ExpiresUtc.Value >= before.AddDays(30).AddSeconds(-2)
                    && p.ExpiresUtc.Value <= after.AddDays(30).AddSeconds(2))),
            Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_WhenRememberMeUnchecked_SignsIntoCookieSchemeWithSessionExpiry()
    {
        var harness = CreateHarness(signInResult: SuccessfulSignIn());

        var result = await harness.Model.OnPostAsync(CancellationToken.None);

        Assert.IsType<Microsoft.AspNetCore.Mvc.SignInResult>(result);
        Assert.False(harness.Model.RememberMe);
        harness.AuthenticationService.Verify(
            s => s.SignInAsync(
                It.IsAny<HttpContext>(),
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.IsAny<ClaimsPrincipal>(),
                It.Is<AuthenticationProperties>(p => !p.IsPersistent && p.ExpiresUtc == null)),
            Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_WhenSignInFails_PreservesRememberMeForReRender()
    {
        var harness = CreateHarness(
            signInResult: FailedSignIn(),
            formOverrides: new Dictionary<string, StringValues> { ["remember_me"] = "true" });

        var result = await harness.Model.OnPostAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.True(harness.Model.RememberMe);
        Assert.Equal("true", redirect.RouteValues!["remember_me"]);
        Assert.Equal("invalid_credentials", redirect.RouteValues!["error"]);
        harness.AuthenticationService.Verify(
            s => s.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_WhenSignInFails_PreservesEveryOpenIddictAuthorizationParameter()
    {
        var harness = CreateHarness(signInResult: FailedSignIn());

        var result = await harness.Model.OnPostAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        var values = redirect.RouteValues!;
        Assert.Equal("invalid_credentials", values["error"]);
        foreach (var (key, expected) in BuildValidAuthorizationForm())
        {
            if (key is "email" or "password" or "remember_me")
            {
                continue;
            }

            Assert.True(values.ContainsKey(key), $"Authorization parameter '{key}' was dropped from the invalid-credentials redirect.");
            Assert.Equal(expected.ToString(), values[key]);
        }
    }

    private static IdentityService.Application.SignIn.SignInResult SuccessfulSignIn() =>
        IdentityService.Application.SignIn.SignInResult.Success(new AuthenticatedUser(
            Guid.NewGuid(),
            "avery@example.com",
            "Avery Nguyen",
            Guid.NewGuid(),
            new[] { UserRoles.TenantAdmin },
            new[] { Permissions.EventsCreate }));

    private static IdentityService.Application.SignIn.SignInResult FailedSignIn() =>
        IdentityService.Application.SignIn.SignInResult.Failure(new[]
        {
            new AuthError(nameof(SignInFailureReason.InvalidCredentials), "The email/password combination is invalid.")
        });

    private static SignInHarness CreateHarness(
        IdentityService.Application.SignIn.SignInResult signInResult,
        Dictionary<string, StringValues>? formOverrides = null)
    {
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<SignInCommand>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(signInResult);

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
        httpContext.Request.Method = HttpMethods.Post;

        var form = BuildValidAuthorizationForm();
        if (formOverrides is not null)
        {
            foreach (var (key, value) in formOverrides)
            {
                form[key] = value;
            }
        }
        httpContext.Request.Form = new FormCollection(form);

        // Mirror what the OpenIddict ASP.NET Core host normally sets after the
        // authorization endpoint runs — tests bypass the middleware.
        var transaction = new OpenIddictServerTransaction { Request = new OpenIddictRequest() };
        httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature { Transaction = transaction });

        var pageContext = new PageContext
        {
            HttpContext = httpContext,
            RouteData = new RouteData(),
            ActionDescriptor = new CompiledPageActionDescriptor()
        };

        var model = new SignInModel(sender.Object, NullLogger<SignInModel>.Instance)
        {
            PageContext = pageContext
        };

        return new SignInHarness(model, authService);
    }

    private static Dictionary<string, StringValues> BuildValidAuthorizationForm() => new()
    {
        ["client_id"] = "spa-client",
        ["redirect_uri"] = "https://app.example.com/callback",
        ["response_type"] = "code",
        ["scope"] = "openid profile email",
        ["state"] = "state-xyz",
        ["nonce"] = "nonce-abc",
        ["code_challenge"] = "challenge",
        ["code_challenge_method"] = "S256",
        ["email"] = "avery@example.com",
        ["password"] = "Password1!"
    };

    private sealed record SignInHarness(SignInModel Model, Mock<IAuthenticationService> AuthenticationService);
}
