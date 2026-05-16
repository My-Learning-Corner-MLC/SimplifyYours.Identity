namespace IdentityService.Contracts.Ping;

public sealed record PingStatusResponse(
    string Message,
    DateTimeOffset CurrentUtcDateTime);
