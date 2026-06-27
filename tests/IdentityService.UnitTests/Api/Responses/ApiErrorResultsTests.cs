using IdentityService.Api.Responses;
using IdentityService.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace IdentityService.UnitTests.Api.Responses;

public sealed class ApiErrorResultsTests
{
    [Fact]
    public void ValidationProblem_GroupsErrorsByCode_AndSetsBadRequestStatus()
    {
        var errors = new[]
        {
            new AuthError("Email", "Email is required."),
            new AuthError("Email", "Email is invalid."),
            new AuthError("Password", "Password is required.")
        };

        var result = ApiErrorResults.ValidationProblem(errors);
        var problem = Assert.IsType<ProblemHttpResult>(result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        var details = Assert.IsType<HttpValidationProblemDetails>(problem.ProblemDetails);
        Assert.Equal(2, details.Errors["Email"].Length);
        Assert.Single(details.Errors["Password"]);
    }

    [Fact]
    public void ValidationProblem_IncludesCorrelationId_WhenContextProvided()
    {
        var context = new DefaultHttpContext();
        context.Items[CorrelationId.HeaderName] = "corr-1";

        var result = ApiErrorResults.ValidationProblem(
            new[] { new AuthError("X", "Y") },
            context);

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.True(problem.ProblemDetails.Extensions.ContainsKey("correlationId"));
        Assert.Equal("corr-1", problem.ProblemDetails.Extensions["correlationId"]);
    }

    [Fact]
    public void ValidationProblem_OmitsCorrelationId_WhenContextIsNull()
    {
        var result = ApiErrorResults.ValidationProblem(new[] { new AuthError("X", "Y") });

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.False(problem.ProblemDetails.Extensions.ContainsKey("correlationId"));
        Assert.True(problem.ProblemDetails.Extensions.ContainsKey("message"));
    }

    [Fact]
    public void CreateProblemDetails_PopulatesAllFields_AndExtensions()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "trace-1" };
        context.Request.Path = "/auth/sign-up";

        var details = ApiErrorResults.CreateProblemDetails(
            context,
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Boom",
            message: "Bad happened.");

        Assert.Equal(StatusCodes.Status500InternalServerError, details.Status);
        Assert.Equal("Boom", details.Title);
        Assert.Equal("Bad happened.", details.Detail);
        Assert.Equal("/auth/sign-up", details.Instance);
        Assert.Equal("Bad happened.", details.Extensions["message"]);
        Assert.Equal("trace-1", details.Extensions["correlationId"]);
    }
}
