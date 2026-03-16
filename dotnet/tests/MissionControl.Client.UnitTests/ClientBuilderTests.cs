namespace MissionControl.Client.UnitTests;

public class ClientBuilderTests
{
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
    public void Build_ReturnsNonNullClient_WhenEnvironmentIsOverridden()
    {
        var client = new ClientBuilder()
            .WithAzureAdClientCredentials("client-id", "client-secret")
            .ForEnvironment(MissionControlEnvironment.Sandbox)
            .Build();

        Assert.NotNull(client);
    }
}
