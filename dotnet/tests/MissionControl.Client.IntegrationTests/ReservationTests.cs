using MissionControl.Client.Generated.Models;

namespace MissionControl.Client.IntegrationTests;


public class ReservationTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact(Skip = "Write test — creates and cancels a live reservation. Remove Skip to run.")]
    public async Task CreateReservation_CanBeCancelled()
    {
        var client = fixture.GetClient();
        var ct = TestContext.Current.CancellationToken;

        var reservation = await client.Reservation.PostAsync(new CreateReservationRequest
        {
            ProductId = fixture.ProductId,
            RegionId = fixture.RegionId,
        }, cancellationToken: ct);

        Assert.NotNull(reservation);
        Assert.NotNull(reservation.Id);
        Assert.Equal(1, reservation.StatusId); // 1 = AwaitingClaim

        var cancelled = await client.Reservation[reservation.Id!.Value].Cancel.PostAsync(cancellationToken: ct);

        Assert.NotNull(cancelled);
        Assert.Equal(3, cancelled.StatusId); // 3 = Cancelled
    }

    [Fact(Skip = "Write test — creates a reservation and claims it as an order. Remove Skip to run.")]
    public async Task CreateReservation_CanBeClaimed()
    {
        var client = fixture.GetClient();
        var ct = TestContext.Current.CancellationToken;

        var reservation = await client.Reservation.PostAsync(new CreateReservationRequest
        {
            ProductId = fixture.ProductId,
            RegionId = fixture.RegionId,
        }, cancellationToken: ct);

        Assert.NotNull(reservation);
        Assert.NotNull(reservation.Id);

        var order = await client.Order.ClaimReservation[reservation.Id!.Value].PostAsync(
            new ClaimReservationRequest
            {
                RequestId = Guid.NewGuid(),
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
