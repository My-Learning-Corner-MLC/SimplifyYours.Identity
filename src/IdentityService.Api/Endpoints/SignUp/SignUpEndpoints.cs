using FluentValidation;
using IdentityService.Application.SignUp;
using IdentityService.Contracts;
using IdentityService.Contracts.SignUp;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace IdentityService.Api.Endpoints.SignUp;

internal static class SignUpEndpoints
{
    public static async Task<Results<Created<SignUpResponse>, BadRequest<AuthErrorResponse>>> SignUpAsync(
        SignUpRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        SignUpResult result;

        try
        {
            result = await sender.Send(SignUpCommand.FromRequest(request), cancellationToken);
        }
        catch (ValidationException exception)
        {
            return TypedResults.BadRequest(new AuthErrorResponse(ToAuthErrors(exception)));
        }

        if (!result.Succeeded)
        {
            return TypedResults.BadRequest(new AuthErrorResponse(result.Errors));
        }

        return TypedResults.Created($"/users/{result.User!.UserId}", result.User);
    }

    private static IReadOnlyCollection<AuthError> ToAuthErrors(ValidationException exception)
    {
        return exception.Errors
            .Select(error => new AuthError(error.PropertyName, error.ErrorMessage))
            .ToArray();
    }
}
