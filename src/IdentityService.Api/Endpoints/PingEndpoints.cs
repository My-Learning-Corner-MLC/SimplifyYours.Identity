using IdentityService.Application.Ping;

namespace IdentityService.Api.Endpoints;

internal static class PingEndpoints
{
    public static IEndpointRouteBuilder MapPingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/ping", (IPingService pingService, ILoggerFactory loggerFactory) =>
            {
                var response = pingService.GetStatus();

                loggerFactory
                    .CreateLogger("IdentityService.Ping")
                    .LogInformation(
                        "Identity Service is up. Current UTC datetime: {CurrentUtcDateTime}",
                        response.CurrentUtcDateTime);

                return Results.Ok(response);
            })
            .WithName("Ping");

        return endpoints;
    }
}
