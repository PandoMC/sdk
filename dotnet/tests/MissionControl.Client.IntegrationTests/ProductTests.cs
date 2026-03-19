using MissionControl.Client.Generated.Models;

namespace MissionControl.Client.IntegrationTests;

public class ProductTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task GetProducts_ReturnsResult()
    {
        var client = fixture.GetClient();

        var result = await client.Product.GetAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
    }

    [Fact]
    public async Task HasStock_ReturnsResponse()
    {
        var client = fixture.GetClient();
        var ct = TestContext.Current.CancellationToken;

        // Discover a product with at least one allowed region rather than requiring extra env vars.
        var products = await client.Product.GetAsync(cancellationToken: ct);
        var product = products?.Result?.FirstOrDefault(p => p.AllowedRegions?.Count > 0);

        if (product is null) Assert.Skip("No products with allowed regions found in the configured environment.");

        var response = await client.Product.HasStock.PostAsync(new HasStockRequest
        {
            ProductId = product.Id,
            RegionId = product.AllowedRegions![0],
        }, cancellationToken: ct);

        Assert.NotNull(response);
        // HasStock is a bool — it being non-null is the correctness signal.
        Assert.NotNull(response.HasStock);
    }
}