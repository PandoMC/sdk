namespace MissionControl.Client.IntegrationTests;

public class PriceListTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task GetPriceList_ReturnsResult()
    {
        var client = fixture.GetClient();

        // Discover the first available product so no extra env var is needed.
        var products = await client.Product.GetAsync(cancellationToken: TestContext.Current.CancellationToken);
        var product = products?.Result?.FirstOrDefault();

        if (product is null) Assert.Skip("No products found in the configured environment.");

        var result = await client.PriceList.GetAsync(q =>
        {
            q.QueryParameters.ProductId   = product.Id;
            q.QueryParameters.PublisherId = product.PublisherId;
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
    }
}
