using IdentityService.Application.Ping;
using Moq;

namespace IdentityService.UnitTests.Ping;

public sealed class PingServiceTests
{
    [Fact]
    public void GetStatus_ReturnsServiceUpMessageWithCurrentUtcDateTime()
    {
        var fixedDateTime = new DateTimeOffset(2026, 5, 16, 8, 30, 45, TimeSpan.Zero);
        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(fixedDateTime);
        var service = new PingService(timeProvider.Object);

        var response = service.GetStatus();

        Assert.Equal("Identity Service is up.", response.Message);
        Assert.Equal(fixedDateTime, response.CurrentUtcDateTime);
        Assert.Equal(TimeSpan.Zero, response.CurrentUtcDateTime.Offset);
        timeProvider.Verify(provider => provider.GetUtcNow(), Times.Once);
    }
}
