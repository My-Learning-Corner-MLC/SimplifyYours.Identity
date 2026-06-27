using IdentityService.Api.Middleware;
using IdentityService.Api.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdentityService.UnitTests.Api.Middleware;

public sealed class RequestLoggingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_UsesHeaderCorrelationId_WhenProvided()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationId.HeaderName] = " incoming-id ";

        var middleware = new RequestLoggingMiddleware(
            _ => Task.CompletedTask,
            NullLogger<RequestLoggingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal("incoming-id", context.Items[CorrelationId.HeaderName]);
        Assert.Equal("incoming-id", context.Response.Headers[CorrelationId.HeaderName].ToString());
    }

    [Fact]
    public async Task InvokeAsync_FallsBackToTraceIdentifier_WhenHeaderMissing()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "trace-42" };

        var middleware = new RequestLoggingMiddleware(
            _ => Task.CompletedTask,
            NullLogger<RequestLoggingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal("trace-42", context.Items[CorrelationId.HeaderName]);
        Assert.Equal("trace-42", context.Response.Headers[CorrelationId.HeaderName].ToString());
    }

    [Theory]
    [InlineData(200)]
    [InlineData(400)]
    [InlineData(500)]
    public async Task InvokeAsync_CompletesAndPropagatesStatus(int statusCode)
    {
        var context = new DefaultHttpContext();

        var middleware = new RequestLoggingMiddleware(
            ctx => { ctx.Response.StatusCode = statusCode; return Task.CompletedTask; },
            NullLogger<RequestLoggingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(statusCode, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_RethrowsAndLogs_WhenNextThrows()
    {
        var context = new DefaultHttpContext();

        var middleware = new RequestLoggingMiddleware(
            _ => throw new InvalidOperationException("boom"),
            NullLogger<RequestLoggingMiddleware>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context));
        Assert.Equal("boom", ex.Message);
    }
}
