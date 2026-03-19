using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using MissionControl.Client.Generated.Models;
using Moq;
using MissionControlClient = MissionControl.Client.Generated.Client;

namespace MissionControl.Client.UnitTests;

public class ProductRequestTests
{
    [Fact]
    public async Task GetAsync_ReturnsProductListResult()
    {
        // Arrange
        var adapter = new Mock<IRequestAdapter>();
        adapter.SetupProperty(a => a.BaseUrl, "https://localhost");
        var client = new MissionControlClient(adapter.Object);

        var expected = new ProductListResult();

        adapter.Setup(a => a.SendAsync(
                It.IsAny<RequestInformation>(),
                ProductListResult.CreateFromDiscriminatorValue,
                It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await client.Product.GetAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetAsync_SendsRequestToProductEndpoint()
    {
        // Arrange
        var adapter = new Mock<IRequestAdapter>();
        adapter.SetupProperty(a => a.BaseUrl, "https://localhost");
        var client = new MissionControlClient(adapter.Object);

        RequestInformation? capturedRequest = null;

        adapter.Setup(a => a.SendAsync(
                It.IsAny<RequestInformation>(),
                ProductListResult.CreateFromDiscriminatorValue,
                It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<RequestInformation, ParsableFactory<ProductListResult>, Dictionary<string, ParsableFactory<IParsable>>, CancellationToken>(
                (req, _, _, _) => capturedRequest = req)
            .ReturnsAsync(new ProductListResult());

        // Act
        await client.Product.GetAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(Method.GET, capturedRequest!.HttpMethod);
        Assert.Contains("/Product", capturedRequest.URI.ToString());
    }

    [Fact]
    public async Task GetAsync_WithQueryParameters_IncludesThemInRequest()
    {
        // Arrange
        var adapter = new Mock<IRequestAdapter>();
        adapter.SetupProperty(a => a.BaseUrl, "https://localhost");
        var client = new MissionControlClient(adapter.Object);

        RequestInformation? capturedRequest = null;

        adapter.Setup(a => a.SendAsync(
                It.IsAny<RequestInformation>(),
                ProductListResult.CreateFromDiscriminatorValue,
                It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<RequestInformation, ParsableFactory<ProductListResult>, Dictionary<string, ParsableFactory<IParsable>>, CancellationToken>(
                (req, _, _, _) => capturedRequest = req)
            .ReturnsAsync(new ProductListResult());

        // Act
        await client.Product.GetAsync(
            config =>
            {
                config.QueryParameters.Name = "Minecraft";
                config.QueryParameters.PageSize = 10;
                config.QueryParameters.PageIndex = 0;
            },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(capturedRequest);
        var uri = capturedRequest!.URI.ToString();
        Assert.Contains("Name=Minecraft", uri);
        Assert.Contains("pageSize=10", uri);
        Assert.Contains("pageIndex=0", uri);
    }
}
