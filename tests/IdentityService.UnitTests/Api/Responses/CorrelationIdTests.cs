using IdentityService.Api.Responses;
using Microsoft.AspNetCore.Http;

namespace IdentityService.UnitTests.Api.Responses;

public sealed class CorrelationIdTests
{
    [Fact]
    public void Get_ReturnsValueFromItems_WhenPresent()
    {
        var context = new DefaultHttpContext();
        context.Items[CorrelationId.HeaderName] = "abc-123";

        var result = CorrelationId.Get(context);

        Assert.Equal("abc-123", result);
    }

    [Fact]
    public void Get_FallsBackToTraceIdentifier_WhenItemMissing()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "trace-xyz" };

        var result = CorrelationId.Get(context);

        Assert.Equal("trace-xyz", result);
    }

    [Fact]
    public void Get_FallsBackToTraceIdentifier_WhenItemIsWhitespace()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "trace-xyz" };
        context.Items[CorrelationId.HeaderName] = "   ";

        var result = CorrelationId.Get(context);

        Assert.Equal("trace-xyz", result);
    }

    [Fact]
    public void Get_FallsBackToTraceIdentifier_WhenItemIsNonString()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "trace-xyz" };
        context.Items[CorrelationId.HeaderName] = 42;

        var result = CorrelationId.Get(context);

        Assert.Equal("trace-xyz", result);
    }
}
