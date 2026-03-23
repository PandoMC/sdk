using System.Text;
using System.Text.Json;

namespace MissionControl.Client;

/// <summary>
/// A <see cref="DelegatingHandler"/> that automatically attempts to cancel an order when a
/// request to one of the order-mutation endpoints fails with a server error (5xx) or a
/// connection/timeout error.
/// <para>
/// Affected endpoints: <c>createOrder</c>, <c>claimReservation</c>, <c>reportOrder</c>.
/// </para>
/// <para>
/// The cancellation attempt is best-effort. If it fails the original error is still propagated
/// to the caller unchanged.
/// </para>
/// </summary>
internal sealed class AutoCancelOrderHandler : DelegatingHandler
{
    /// <summary>
    /// Maximum time to wait for the compensating cancel request to complete.
    /// </summary>
    private static readonly TimeSpan CancelTimeout = TimeSpan.FromSeconds(30);


    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!IsAutoCancelEndpoint(request))
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        // Buffer the request body before SendAsync consumes the stream.
        // ReadAsStringAsync calls LoadIntoBufferAsync internally, so the content remains
        // sendable by the inner handlers.
        string? requestBody = null;
        if (request.Content is not null)
            requestBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Connection error or timeout — attempt to cancel with a fresh token so that
            // the cancel request is not affected by the original token's cancellation.
            await TryCancelOrderAsync(request, requestBody).ConfigureAwait(false);
            throw;
        }

        if (IsServerError(response))
            await TryCancelOrderAsync(request, requestBody).ConfigureAwait(false);

        return response;
    }

    private static bool IsAutoCancelEndpoint(HttpRequestMessage request)
    {
        var path = request.RequestUri?.AbsolutePath;
        if (path is null)
            return false;

        // Use Contains so this works regardless of any base-path prefix
        // (e.g. /partnerApi/v2/Order/createOrder).
        foreach (var segment in NonIdempotentPaths.All)
        {
            if (path.Contains(segment, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool IsServerError(HttpResponseMessage response) =>
        (int)response.StatusCode >= 500;

    private async Task TryCancelOrderAsync(HttpRequestMessage originalRequest, string? requestBody)
    {
        try
        {
            var requestId = ExtractRequestId(requestBody);
            if (requestId is null)
                return;

            var cancelUri = BuildCancelUri(originalRequest);
            if (cancelUri is null)
                return;

            using var cancelRequest = new HttpRequestMessage(HttpMethod.Post, cancelUri);
            cancelRequest.Content = new StringContent(
                JsonSerializer.Serialize(new { originalRequestId = requestId }),
                Encoding.UTF8,
                "application/json");

            // Copy the auth header so the cancel request authenticates with the same token.
            if (originalRequest.Headers.Authorization is { } auth)
                cancelRequest.Headers.Authorization = auth;

            // Copy the partner-id header in case no PartnerIdHandler is registered.
            if (originalRequest.Headers.TryGetValues(MissionControlHeaders.PartnerId, out var partnerIds))
                cancelRequest.Headers.TryAddWithoutValidation(MissionControlHeaders.PartnerId, partnerIds);

            using var cts = new CancellationTokenSource(CancelTimeout);
            await base.SendAsync(cancelRequest, cts.Token).ConfigureAwait(false);
        }
        catch
        {
            // Best-effort: swallow failures so the original error propagates.
        }
    }

    private static Guid? ExtractRequestId(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("requestId", out var prop) && prop.TryGetGuid(out var id))
                return id;
        }
        catch (JsonException)
        {
            // Malformed body — nothing to extract.
        }

        return null;
    }

    private static Uri? BuildCancelUri(HttpRequestMessage request)
    {
        if (request.RequestUri is null)
            return null;

        var path = request.RequestUri.AbsolutePath;

        // Find the "/Order/" segment and replace everything after it with "cancelOrder".
        var orderIndex = path.IndexOf("/Order/", StringComparison.OrdinalIgnoreCase);
        if (orderIndex < 0)
            return null;

        var newPath = string.Concat(path.AsSpan(0, orderIndex + "/Order/".Length), "cancelOrder");
        var builder = new UriBuilder(request.RequestUri) { Path = newPath, Query = string.Empty };
        return builder.Uri;
    }
}
