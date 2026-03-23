using MissionControl.Client.Generated.Models;

namespace MissionControl.Client.IntegrationTests;

public class OrderTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task GetOrders_ReturnsResult()
    {
        var client = fixture.GetClient();
        var ct = TestContext.Current.CancellationToken;

        var result = await client.Order.GetAsync(
            q => q.QueryParameters.PublisherId = fixture.PublisherId,
            cancellationToken: ct);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
    }

    [Fact(Skip = "Write test — creates a direct order and expects a fulfilled key. Remove Skip to run.")]
    public async Task CreateOrder_ReturnsOrderWithKey()
    {
        var client = fixture.GetClient();
        var ct = TestContext.Current.CancellationToken;

        var order = await client.Order.CreateOrder.PostAsync(new CreateOrderRequest
        {
            RequestId = Guid.NewGuid(),
            ProductId = fixture.ProductId,
            RegionId = fixture.RegionId,
            PartnerReference = $"integration-test-{Guid.NewGuid():N}",
            InvoiceDate = DateTimeOffset.UtcNow,
            SalesChannel = [],
            EndConsumer = new Consumer
            {
                IpAddress = "127.0.0.1",
                Language = "en-US",
                Location = new Location { CountryCode = "NL" },
            },
            SalesPrice = new SalesPriceRequest
            {
                PriceIncludingVat = new Money { Value = 9.99, CurrencyCode = "EUR" },
                VatDetail = new VatDetail { Rate = 21 },
            },
        }, cancellationToken: ct);

        Assert.NotNull(order);
        Assert.NotNull(order.Id);
        Assert.Equal(2, order.StatusId); // 2 = Fulfilled
        Assert.NotNull(order.Key);
    }

    [Fact(Skip = "Write test — creates an order and cancels it immediately. Remove Skip to run.")]
    public async Task CreateOrder_CanBeCancelled()
    {
        var client = fixture.GetClient();
        var ct = TestContext.Current.CancellationToken;

        var requestId = Guid.NewGuid();

        var order = await client.Order.CreateOrder.PostAsync(new CreateOrderRequest
        {
            RequestId = requestId,
            ProductId = fixture.ProductId,
            RegionId = fixture.RegionId,
            PartnerReference = $"integration-test-{Guid.NewGuid():N}",
            InvoiceDate = DateTimeOffset.UtcNow,
            SalesChannel = [],
            EndConsumer = new Consumer
            {
                IpAddress = "127.0.0.1",
                Language = "en-US",
                Location = new Location { CountryCode = "NL" },
            },
            SalesPrice = new SalesPriceRequest
            {
                PriceIncludingVat = new Money { Value = 9.99, CurrencyCode = "EUR" },
                VatDetail = new VatDetail { Rate = 21 },
            },
        }, cancellationToken: ct);

        Assert.NotNull(order);
        Assert.NotNull(order.Id);

        var cancelled = await client.Order.CancelOrder.PostAsync(new CancelOrderRequest
        {
            OriginalRequestId = requestId,
        }, cancellationToken: ct);

        Assert.NotNull(cancelled);
        Assert.Equal(3, cancelled.StatusId); // 3 = Cancelled
    }

    [Fact(Skip = "Write test — reports an externally sold order (no key delivered). Remove Skip to run.")]
    public async Task ReportOrder_ReturnsOrder()
    {
        var client = fixture.GetClient();
        var ct = TestContext.Current.CancellationToken;

        var order = await client.Order.ReportOrder.PostAsync(new ReportOrderRequest
        {
            RequestId = Guid.NewGuid(),
            ProductId = fixture.ProductId,
            RegionId = fixture.RegionId,
            PartnerReference = $"integration-test-{Guid.NewGuid():N}",
            InvoiceDate = DateTimeOffset.UtcNow,
            SalesChannel = [],
            EndConsumer = new Consumer
            {
                IpAddress = "127.0.0.1",
                Language = "en-US",
                Location = new Location { CountryCode = "NL" },
            },
            SalesPrice = new SalesPriceRequest
            {
                PriceIncludingVat = new Money { Value = 9.99, CurrencyCode = "EUR" },
                VatDetail = new VatDetail { Rate = 21 },
            },
        }, cancellationToken: ct);

        Assert.NotNull(order);
        Assert.NotNull(order.Id);
    }
}