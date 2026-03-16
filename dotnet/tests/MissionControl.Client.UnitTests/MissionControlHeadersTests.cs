namespace MissionControl.Client.UnitTests;

public class MissionControlHeadersTests
{
    [Fact]
    public void PartnerId_HasExpectedValue()
    {
        Assert.Equal("X-Partner-Id", MissionControlHeaders.PartnerId);
    }
}
