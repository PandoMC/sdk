namespace MissionControl.Client.IntegrationTests;

public class PartnerTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task GetPartners_ReturnsResult()
    {
        var client = fixture.GetClient();

        var result = await client.Partner.GetAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
    }
}
