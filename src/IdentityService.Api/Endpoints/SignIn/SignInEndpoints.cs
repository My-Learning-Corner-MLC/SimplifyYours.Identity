using System.Security.Claims;
using FluentValidation;
using IdentityService.Application.SignIn;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace IdentityService.Api.Endpoints.SignIn;

internal static class SignInEndpoints
{
    public static async Task<IResult> SignInAsync(
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var openIddictRequest = httpContext.GetOpenIddictServerRequest();

        if (openIddictRequest is null || !openIddictRequest.IsPasswordGrantType())
        {
            return ForbidInvalidGrant("The specified grant type is not supported.");
        }

        SignInResult result;

        try
        {
            result = await sender.Send(
                new SignInCommand(openIddictRequest.Username, openIddictRequest.Password),
                cancellationToken);
        }
        catch (ValidationException)
        {
            return ForbidInvalidGrant("The email/password combination is invalid.");
        }

        if (!result.Succeeded)
        {
            return ForbidInvalidGrant("The email/password combination is invalid.");
        }

        var principal = CreatePrincipal(result.User!, openIddictRequest);

        return TypedResults.SignIn(
            principal,
            authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static ClaimsPrincipal CreatePrincipal(
        AuthenticatedUser user,
        OpenIddictRequest openIddictRequest)
    {
        var identity = new ClaimsIdentity(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            Claims.Name,
            Claims.Role);

        identity.SetClaim(Claims.Subject, user.UserId.ToString());
        identity.SetClaim(Claims.Email, user.Email);
        identity.SetClaim(Claims.Name, user.FullName);

        foreach (var role in user.Roles)
        {
            identity.AddClaim(Claims.Role, role);
        }

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(openIddictRequest.GetScopes());
        principal.SetResources("simplify-yours-api");

        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim));
        }

        return principal;
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        yield return Destinations.AccessToken;

        if (claim.Type is Claims.Email or Claims.Name or Claims.Role)
        {
            yield return Destinations.IdentityToken;
        }
    }

    private static IResult ForbidInvalidGrant(string description)
    {
        var properties = new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
        });

        return TypedResults.Forbid(
            properties,
            new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });
    }
}
