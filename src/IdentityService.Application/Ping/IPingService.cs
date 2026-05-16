using IdentityService.Contracts.Ping;

namespace IdentityService.Application.Ping;

public interface IPingService
{
    PingStatusResponse GetStatus();
}
