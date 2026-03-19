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

    [Fact]
    public async Task GetAsync_ThrowsProblemDetails_WhenApiReturnsValidationError()
    {
        // Arrange
        var adapter = new Mock<IRequestAdapter>();
        adapter.SetupProperty(a => a.BaseUrl, "https://localhost");
        var client = new MissionControlClient(adapter.Object);

        var validationError = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Title = "One or more validation errors occurred.",
            Status = 400,
            AdditionalData = new Dictionary<string, object>
            {
                ["errors"] = new Dictionary<string, string[]>
                {
                    ["ProductId"] = ["The value 'foobar' is not valid."]
                },
                ["traceId"] = "00-90f2dadbc2e77ae87cbe3b8a25fe0689-dbff6f5f82d028d2-00",
            },
        };

        adapter.Setup(a => a.SendAsync(
                It.IsAny<RequestInformation>(),
                ProductListResult.CreateFromDiscriminatorValue,
                It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationError);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ProblemDetails>(() =>
            client.Product.GetAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(400, ex.Status);
        Assert.Equal("One or more validation errors occurred.", ex.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", ex.Type);

        var errors = Assert.IsType<Dictionary<string, string[]>>(ex.AdditionalData["errors"]);
        Assert.Equal(["The value 'foobar' is not valid."], errors["ProductId"]);
        Assert.Equal("00-90f2dadbc2e77ae87cbe3b8a25fe0689-dbff6f5f82d028d2-00", ex.AdditionalData["traceId"]);
    }
}
