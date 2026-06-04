using IdentityService.Api.Endpoints.SignUp;

namespace IdentityService.Api.Endpoints;

internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth").WithTags("Auth");

        group
            .MapPost("sign-up", SignUpEndpoints.SignUpAsync)
            .WithName("SignUp")
            .AllowAnonymous();

        // OpenIddict handles /auth/sign-in and /auth/token. Razor Pages provide
        // the hosted sign-in UI through authorization endpoint passthrough.

        return endpoints;
    }
}
