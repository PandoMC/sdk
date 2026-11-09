namespace MissionControl.Client.IntegrationTests;

public class AssetPackTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task GetAssetPacks_ReturnsResult()
    {
        if (!fixture.HasAssetsBaseUrl) Assert.Skip("MC_ASSETS_BASE_URL is not configured.");

        var client = fixture.GetClient();

        var result = await client.Assets.AssetPack[fixture.PublisherId][fixture.ProductId]
            .GetAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
    }

    [Fact]
    public async Task GetAssetPack_ReturnsAssets()
    {
        if (!fixture.HasAssetsBaseUrl) Assert.Skip("MC_ASSETS_BASE_URL is not configured.");

        var client = fixture.GetClient();
        var ct = TestContext.Current.CancellationToken;

        var packs = await client.Assets.AssetPack[fixture.PublisherId][fixture.ProductId].GetAsync(cancellationToken: ct);
        var pack = packs?.Result?.FirstOrDefault();

        if (pack?.Id is null) Assert.Skip("The configured product has no asset packs.");

        var detail = await client.Assets.AssetPack[fixture.PublisherId][fixture.ProductId][pack.Id]
            .GetAsync(cancellationToken: ct);

        Assert.NotNull(detail);
        Assert.Equal(pack.Id, detail.Id);
        Assert.NotNull(detail.Assets);
    }
}
