using IdentityService.Api.Endpoints.SignIn;
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

        group
            .MapPost("sign-in", SignInEndpoints.SignInAsync)
            .WithName("SignIn")
            .AllowAnonymous();

        return endpoints;
    }
}
