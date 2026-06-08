using FluentValidation;
using IdentityService.Api.Responses;
using IdentityService.Application.SignUp;
using IdentityService.Contracts;
using IdentityService.Contracts.SignUp;
using MediatR;

namespace IdentityService.Api.Endpoints.SignUp;

internal static class SignUpEndpoints
{
    public static async Task<IResult> SignUpAsync(
        SignUpRequest request,
        HttpContext httpContext,
        ISender sender,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("IdentityService.SignUp");
        SignUpResult result;

        try
        {
            result = await sender.Send(SignUpCommand.FromRequest(request), cancellationToken);
        }
        catch (ValidationException exception)
        {
            logger.LogWarning(
                "Sign-up request failed validation. ErrorCount: {ErrorCount}.",
                exception.Errors.Count());
            return ApiErrorResults.ValidationProblem(ToAuthErrors(exception), httpContext);
        }

        if (!result.Succeeded)
        {
            logger.LogWarning(
                "Sign-up request was rejected. ErrorCount: {ErrorCount}.",
                result.Errors.Count);
            return ApiErrorResults.ValidationProblem(result.Errors, httpContext);
        }

        logger.LogInformation("User signed up. UserId: {UserId}.", result.User!.UserId);

        return TypedResults.Created($"/users/{result.User!.UserId}", result.User);
    }

    private static IReadOnlyCollection<AuthError> ToAuthErrors(ValidationException exception)
    {
        return exception.Errors
            .Select(error => new AuthError(error.PropertyName, error.ErrorMessage))
            .ToArray();
    }
}
