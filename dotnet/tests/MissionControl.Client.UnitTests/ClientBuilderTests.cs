namespace MissionControl.Client.UnitTests;

public class ClientBuilderTests
{
    private static readonly Guid PublisherId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    private static string AssetPacksUrl(Generated.Client client) =>
        client.Assets.AssetPack[PublisherId][ProductId].ToGetRequestInformation().URI.ToString();

    [Fact]
    public void Build_ThrowsInvalidOperationException_WhenNoCredentialsConfigured()
    {
        var builder = new ClientBuilder();

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithDefaultPartnerId_ThrowsArgumentException_ForNullOrWhitespace(string? partnerId)
    {
        var builder = new ClientBuilder();

        Assert.ThrowsAny<ArgumentException>(() => builder.WithDefaultPartnerId(partnerId!));
    }

    [Fact]
    public void Build_ReturnsNonNullClient_WhenCredentialsProvided()
    {
        var client = new ClientBuilder()
            .WithAzureAdClientCredentials("client-id", "client-secret")
            .Build();

        Assert.NotNull(client);
    }

    [Fact]
    public void Build_ReturnsNonNullClient_WhenDefaultPartnerIdIsSet()
    {
        var client = new ClientBuilder()
            .WithAzureAdClientCredentials("client-id", "client-secret")
            .WithDefaultPartnerId("partner-123")
            .Build();

        Assert.NotNull(client);
    }

    [Fact]
    public void Build_ConfiguresAssetsClient_WithEnvironmentBaseUrl()
    {
        var client = new ClientBuilder()
            .WithAzureAdClientCredentials("client-id", "client-secret")
            .ForEnvironment(MissionControlEnvironment.Sandbox)
            .Build();

        Assert.Equal("https://api.sandbox.missioncontrol.io/partnerApi/v2/Product", client.Product.ToGetRequestInformation().URI.ToString());
        Assert.Equal($"https://api.sandbox.missioncontrol.io/assets/partnerApi/v1/AssetPack/{PublisherId}/{ProductId}", AssetPacksUrl(client));
    }

    [Fact]
    public void Build_ConfiguresAssetsClient_WithOverriddenBaseUrl()
    {
        var client = new ClientBuilder()
            .WithAzureAdClientCredentials("client-id", "client-secret")
            .WithAssetsBaseUrl("http://localhost:5001/partnerApi/v1")
            .Build();

        Assert.Equal($"http://localhost:5001/partnerApi/v1/AssetPack/{PublisherId}/{ProductId}", AssetPacksUrl(client));
    }

    [Fact]
    public void Assets_Throws_WhenClientNotCreatedByBuilder()
    {
        var client = new Generated.Client(new Microsoft.Kiota.Http.HttpClientLibrary.HttpClientRequestAdapter(
            new Microsoft.Kiota.Abstractions.Authentication.AnonymousAuthenticationProvider()));

        Assert.Throws<InvalidOperationException>(() => client.Assets);
    }

    [Fact]
    public void Build_ReturnsNonNullClient_WhenEnvironmentIsOverridden()
    {
        var client = new ClientBuilder()
            .WithAzureAdClientCredentials("client-id", "client-secret")
            .ForEnvironment(MissionControlEnvironment.Sandbox)
            .Build();

        Assert.NotNull(client);
    }
}
