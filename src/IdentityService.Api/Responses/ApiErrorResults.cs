using IdentityService.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.Responses;

internal static class ApiErrorResults
{
    public static IResult ValidationProblem(IReadOnlyCollection<AuthError> errors, HttpContext? context = null)
    {
        const string message = "Some information is missing or invalid. Please check the highlighted fields and try again.";
        var validationErrors = errors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Message).ToArray());

        return Results.ValidationProblem(
            validationErrors,
            statusCode: StatusCodes.Status400BadRequest,
            title: "Please check your request.",
            detail: message,
            extensions: CreateExtensions(message, context));
    }

    public static ProblemDetails CreateProblemDetails(HttpContext context, int statusCode, string title, string message)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = message,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["message"] = message;
        problemDetails.Extensions["correlationId"] = CorrelationId.Get(context);

        return problemDetails;
    }

    private static Dictionary<string, object?> CreateExtensions(string message, HttpContext? context)
    {
        var extensions = new Dictionary<string, object?>
        {
            ["message"] = message
        };

        if (context is not null)
        {
            extensions["correlationId"] = CorrelationId.Get(context);
        }

        return extensions;
    }
}
