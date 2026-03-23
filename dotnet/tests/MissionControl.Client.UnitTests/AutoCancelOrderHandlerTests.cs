using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MissionControl.Client.UnitTests;

public class AutoCancelOrderHandlerTests
{
    private static readonly Guid TestRequestId = Guid.Parse("aaaabbbb-cccc-dddd-eeee-ffffffffffff");

    [Fact]
    public async Task SuccessfulRequest_DoesNotSendCancelRequest()
    {
        var (handler, inner) = CreatePipeline();
        inner.EnqueueResponse(HttpStatusCode.OK);

        using var client = new HttpClient(handler);
        using var request = CreateOrderRequest("/Order/createOrder", TestRequestId);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(inner.SentRequests);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task ServerError_SendsCancelRequest(HttpStatusCode statusCode)
    {
        var (handler, inner) = CreatePipeline();
        inner.EnqueueResponse(statusCode);
        inner.EnqueueResponse(HttpStatusCode.OK);

        using var client = new HttpClient(handler);
        using var request = CreateOrderRequest("/Order/createOrder", TestRequestId);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(statusCode, response.StatusCode);
        Assert.Equal(2, inner.SentRequests.Count);
        AssertIsCancelRequest(inner.SentRequests[1], TestRequestId);
    }

    [Fact]
    public async Task ClientError_DoesNotSendCancelRequest()
    {
        var (handler, inner) = CreatePipeline();
        inner.EnqueueResponse(HttpStatusCode.BadRequest);

        using var client = new HttpClient(handler);
        using var request = CreateOrderRequest("/Order/createOrder", TestRequestId);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Single(inner.SentRequests);
    }

    [Fact]
    public async Task ConnectionError_SendsCancelRequestAndRethrows()
    {
        var (handler, inner) = CreatePipeline();
        inner.EnqueueResponse(_ => Task.FromException<HttpResponseMessage>(
            new HttpRequestException("Connection refused")));
        inner.EnqueueResponse(HttpStatusCode.OK);

        using var client = new HttpClient(handler);
        using var request = CreateOrderRequest("/Order/createOrder", TestRequestId);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.SendAsync(request, TestContext.Current.CancellationToken));

        Assert.Equal(2, inner.SentRequests.Count);
        AssertIsCancelRequest(inner.SentRequests[1], TestRequestId);
    }

    [Fact]
    public async Task Timeout_SendsCancelRequestAndRethrows()
    {
        var (handler, inner) = CreatePipeline();
        inner.EnqueueResponse(_ => Task.FromException<HttpResponseMessage>(
            new TaskCanceledException("Request timed out")));
        inner.EnqueueResponse(HttpStatusCode.OK);

        using var client = new HttpClient(handler);
        using var request = CreateOrderRequest("/Order/createOrder", TestRequestId);

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => client.SendAsync(request, TestContext.Current.CancellationToken));

        Assert.Equal(2, inner.SentRequests.Count);
        AssertIsCancelRequest(inner.SentRequests[1], TestRequestId);
    }

    [Fact]
    public async Task NonOrderEndpoint_DoesNotSendCancelRequestOnError()
    {
        var (handler, inner) = CreatePipeline();
        inner.EnqueueResponse(HttpStatusCode.InternalServerError);

        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test.com/Product");
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Single(inner.SentRequests);
    }

    [Fact]
    public async Task ClaimReservation_SendsCancelRequestOnError()
    {
        var (handler, inner) = CreatePipeline();
        inner.EnqueueResponse(HttpStatusCode.InternalServerError);
        inner.EnqueueResponse(HttpStatusCode.OK);

        using var client = new HttpClient(handler);
        using var request = CreateOrderRequest(
            $"/Order/claimReservation/{Guid.NewGuid()}", TestRequestId);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(2, inner.SentRequests.Count);
        AssertIsCancelRequest(inner.SentRequests[1], TestRequestId);
    }

    [Fact]
    public async Task ReportOrder_SendsCancelRequestOnError()
    {
        var (handler, inner) = CreatePipeline();
        inner.EnqueueResponse(HttpStatusCode.InternalServerError);
        inner.EnqueueResponse(HttpStatusCode.OK);

        using var client = new HttpClient(handler);
        using var request = CreateOrderRequest("/Order/reportOrder", TestRequestId);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(2, inner.SentRequests.Count);
        AssertIsCancelRequest(inner.SentRequests[1], TestRequestId);
    }

    [Fact]
    public async Task MissingRequestId_DoesNotSendCancelRequest()
    {
        var (handler, inner) = CreatePipeline();
        inner.EnqueueResponse(HttpStatusCode.InternalServerError);

        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(
            HttpMethod.Post, "https://api.test.com/Order/createOrder");
        request.Content = new StringContent(
            """{"productId": "aaaabbbb-cccc-dddd-eeee-ffffffffffff"}""",
            Encoding.UTF8,
            "application/json");

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Single(inner.SentRequests);
    }

    [Fact]
    public async Task CancelRequestFails_OriginalErrorStillReturned()
    {
        var (handler, inner) = CreatePipeline();
        inner.EnqueueResponse(HttpStatusCode.InternalServerError);
        inner.EnqueueResponse(_ => Task.FromException<HttpResponseMessage>(
            new HttpRequestException("Cancel failed")));

        using var client = new HttpClient(handler);
        using var request = CreateOrderRequest("/Order/createOrder", TestRequestId);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task CopiesAuthAndPartnerHeaders()
    {
        var (handler, inner) = CreatePipeline();
        inner.EnqueueResponse(HttpStatusCode.InternalServerError);
        inner.EnqueueResponse(HttpStatusCode.OK);

        using var client = new HttpClient(handler);
        using var request = CreateOrderRequest("/Order/createOrder", TestRequestId);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");
        request.Headers.TryAddWithoutValidation(MissionControlHeaders.PartnerId, "partner-123");

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        var cancelRequest = inner.SentRequests[1].Request;
        Assert.Equal("Bearer", cancelRequest.Headers.Authorization?.Scheme);
        Assert.Equal("test-token", cancelRequest.Headers.Authorization?.Parameter);
        Assert.Contains("partner-123", cancelRequest.Headers.GetValues(MissionControlHeaders.PartnerId));
    }

    [Fact]
    public async Task CancelUri_PointsToCorrectEndpoint()
    {
        var (handler, inner) = CreatePipeline();
        inner.EnqueueResponse(HttpStatusCode.InternalServerError);
        inner.EnqueueResponse(HttpStatusCode.OK);

        using var client = new HttpClient(handler);
        using var request = CreateOrderRequest("/partnerApi/v2/Order/createOrder", TestRequestId);

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        var cancelUri = inner.SentRequests[1].Request.RequestUri!;
        Assert.Equal("/partnerApi/v2/Order/cancelOrder", cancelUri.AbsolutePath);
    }

    // -- Helpers --

    private static (AutoCancelOrderHandler Handler, RecordingHandler Inner) CreatePipeline()
    {
        var inner = new RecordingHandler();
        var handler = new AutoCancelOrderHandler { InnerHandler = inner };
        return (handler, inner);
    }

    private static HttpRequestMessage CreateOrderRequest(string path, Guid requestId)
    {
        var body = JsonSerializer.Serialize(new { requestId });
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.test.com{path}")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        return request;
    }

    private static void AssertIsCancelRequest(
        (HttpRequestMessage Request, string? Body) captured, Guid expectedRequestId)
    {
        Assert.Contains("/Order/cancelOrder", captured.Request.RequestUri!.AbsolutePath);
        Assert.NotNull(captured.Body);

        using var doc = JsonDocument.Parse(captured.Body);
        Assert.True(doc.RootElement.TryGetProperty("originalRequestId", out var prop));
        Assert.Equal(expectedRequestId, prop.GetGuid());
    }

    /// <summary>
    /// An <see cref="HttpMessageHandler"/> that records all requests and returns queued responses.
    /// </summary>
    internal sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly List<(HttpRequestMessage Request, string? Body)> _sent = [];
        private readonly Queue<Func<HttpRequestMessage, Task<HttpResponseMessage>>> _responses = new();

        public IReadOnlyList<(HttpRequestMessage Request, string? Body)> SentRequests => _sent;

        public void EnqueueResponse(HttpStatusCode statusCode) =>
            _responses.Enqueue(_ => Task.FromResult(new HttpResponseMessage(statusCode)));

        public void EnqueueResponse(Func<HttpRequestMessage, Task<HttpResponseMessage>> factory) =>
            _responses.Enqueue(factory);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string? body = null;
            if (request.Content is not null)
                body = await request.Content.ReadAsStringAsync(cancellationToken);

            _sent.Add((request, body));

            if (_responses.TryDequeue(out var handler))
                return await handler(request);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
