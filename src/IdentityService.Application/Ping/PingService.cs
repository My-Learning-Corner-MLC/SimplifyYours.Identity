using IdentityService.Contracts.Ping;

namespace IdentityService.Application.Ping;

public sealed class PingService(TimeProvider timeProvider) : IPingService
{
    private const string ServiceUpMessage = "Identity Service is up.";

    public PingStatusResponse GetStatus()
    {
        var currentUtcDateTime = timeProvider.GetUtcNow();

        return new PingStatusResponse(ServiceUpMessage, currentUtcDateTime);
    }
}
