namespace MissionControl.Client.IntegrationTests;

public class RegionTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task GetRegions_ReturnsResult()
    {
        var client = fixture.GetClient();

        var result = await client.Region.GetAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
    }
}
