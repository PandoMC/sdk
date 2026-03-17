using System.Net;

namespace MissionControl.Client.UnitTests;

public class PartnerIdHandlerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsArgumentException_ForNullOrWhitespacePartnerId(string? partnerId)
    {
        Assert.ThrowsAny<ArgumentException>(() => new PartnerIdHandler(partnerId!));
    }

    [Fact]
    public async Task SendAsync_AddsPartnerIdHeader_WhenNotPresent()
    {
        HttpRequestMessage? captured = null;
        var handler = new PartnerIdHandler("partner-123")
        {
            InnerHandler = new FakeHandler(req =>
            {
                captured = req;
                return new HttpResponseMessage(HttpStatusCode.OK);
            })
        };
        var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test", TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
        Assert.True(captured!.Headers.Contains(MissionControlHeaders.PartnerId));
        Assert.Equal("partner-123", captured.Headers.GetValues(MissionControlHeaders.PartnerId).Single());
    }

    [Fact]
    public async Task SendAsync_DoesNotOverwriteExistingPartnerIdHeader()
    {
        HttpRequestMessage? captured = null;
        var handler = new PartnerIdHandler("default-partner")
        {
            InnerHandler = new FakeHandler(req =>
            {
                captured = req;
                return new HttpResponseMessage(HttpStatusCode.OK);
            })
        };
        var client = new HttpClient(handler);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");
        request.Headers.TryAddWithoutValidation(MissionControlHeaders.PartnerId, "override-partner");
        await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
        Assert.Equal("override-partner", captured!.Headers.GetValues(MissionControlHeaders.PartnerId).Single());
    }
}

internal sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(respond(request));
}
