using IdentityService.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

namespace IdentityService.UnitTests.Api.Middleware;

public sealed class HostedSignInRequestValidationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_RewritesPath_WhenRequiredParameterMissing()
    {
        var context = CreateGetSignInRequest(query: new Dictionary<string, StringValues>
        {
            // missing client_id
            ["redirect_uri"] = "https://x",
            ["response_type"] = "code",
            ["scope"] = "openid",
            ["state"] = "s",
            ["nonce"] = "n",
            ["code_challenge"] = "c",
            ["code_challenge_method"] = "S256"
        });

        var nextCalled = false;
        var middleware = new HostedSignInRequestValidationMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            NullLogger<HostedSignInRequestValidationMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal("/auth/sign-in/request-error", context.Request.Path.Value);
        var message = Assert.IsType<string>(
            context.Items[HostedSignInRequestValidationMiddleware.ErrorMessageItemKey]);
        Assert.Contains("client_id", message);
    }

    [Fact]
    public async Task InvokeAsync_RewritesPath_WhenResponseTypeIsNotCode()
    {
        var context = CreateGetSignInRequest(query: ValidQuery(("response_type", "token")));

        var middleware = new HostedSignInRequestValidationMiddleware(
            _ => Task.CompletedTask,
            NullLogger<HostedSignInRequestValidationMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal("/auth/sign-in/request-error", context.Request.Path.Value);
        var message = Assert.IsType<string>(
            context.Items[HostedSignInRequestValidationMiddleware.ErrorMessageItemKey]);
        Assert.Contains("response_type=code", message);
    }

    [Fact]
    public async Task InvokeAsync_RewritesPath_WhenCodeChallengeMethodInvalid()
    {
        var context = CreateGetSignInRequest(query: ValidQuery(("code_challenge_method", "plain")));

        var middleware = new HostedSignInRequestValidationMiddleware(
            _ => Task.CompletedTask,
            NullLogger<HostedSignInRequestValidationMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal("/auth/sign-in/request-error", context.Request.Path.Value);
        var message = Assert.IsType<string>(
            context.Items[HostedSignInRequestValidationMiddleware.ErrorMessageItemKey]);
        Assert.Contains("code_challenge_method=S256", message);
    }

    [Fact]
    public async Task InvokeAsync_KeepsOriginalPath_WhenQueryValid()
    {
        var context = CreateGetSignInRequest(query: ValidQuery());

        var middleware = new HostedSignInRequestValidationMiddleware(
            _ => Task.CompletedTask,
            NullLogger<HostedSignInRequestValidationMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal("/auth/sign-in", context.Request.Path.Value);
        Assert.False(context.Items.ContainsKey(
            HostedSignInRequestValidationMiddleware.ErrorMessageItemKey));
    }

    [Fact]
    public async Task InvokeAsync_DoesNotRunValidation_ForPost()
    {
        var context = CreateGetSignInRequest(query: new Dictionary<string, StringValues>());
        context.Request.Method = HttpMethods.Post;

        var middleware = new HostedSignInRequestValidationMiddleware(
            _ => Task.CompletedTask,
            NullLogger<HostedSignInRequestValidationMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal("/auth/sign-in", context.Request.Path.Value);
        Assert.False(context.Items.ContainsKey(
            HostedSignInRequestValidationMiddleware.ErrorMessageItemKey));
    }

    [Fact]
    public async Task InvokeAsync_DoesNotRunValidation_ForOtherPaths()
    {
        var context = CreateGetSignInRequest(query: new Dictionary<string, StringValues>());
        context.Request.Path = "/something-else";

        var middleware = new HostedSignInRequestValidationMiddleware(
            _ => Task.CompletedTask,
            NullLogger<HostedSignInRequestValidationMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal("/something-else", context.Request.Path.Value);
        Assert.False(context.Items.ContainsKey(
            HostedSignInRequestValidationMiddleware.ErrorMessageItemKey));
    }

    private static HttpContext CreateGetSignInRequest(Dictionary<string, StringValues> query)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/auth/sign-in";
        context.Request.QueryString = QueryString.Create(query.SelectMany(kv =>
            kv.Value.Select(v => new KeyValuePair<string, string?>(kv.Key, v))));
        return context;
    }

    private static Dictionary<string, StringValues> ValidQuery(params (string Key, string Value)[] overrides)
    {
        var query = new Dictionary<string, StringValues>
        {
            ["client_id"] = "spa-client",
            ["redirect_uri"] = "https://app.example.com/callback",
            ["response_type"] = "code",
            ["scope"] = "openid",
            ["state"] = "s",
            ["nonce"] = "n",
            ["code_challenge"] = "c",
            ["code_challenge_method"] = "S256"
        };
        foreach (var (k, v) in overrides)
        {
            query[k] = v;
        }
        return query;
    }
}
