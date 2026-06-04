using System.Security.Claims;
using IdentityService.Application.SignIn;
using IdentityService.Contracts;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace IdentityService.Api.Auth;

internal static class OpenIddictClaimsPrincipalFactory
{
    private const string ResourceName = "simplify-yours-api";

    public static ClaimsPrincipal Create(
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
        principal.SetResources(ResourceName);

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
}
