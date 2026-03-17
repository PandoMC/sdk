namespace MissionControl.Client.IntegrationTests;

public class PublisherTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task GetPublishers_ReturnsResult()
    {
        var client = fixture.GetClient();

        var result = await client.Publisher.GetAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
    }
}
