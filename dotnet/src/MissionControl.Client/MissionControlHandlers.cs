using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using System.Net;

namespace MissionControl.Client;

internal static class MissionControlHandlers
{
    internal static List<DelegatingHandler> Create(string? defaultPartnerId)
    {
        var retryOptions = new RetryHandlerOption
        {
            ShouldRetry = (_, _, response) =>
            {
                if (IsNoRetryEndpoint(response))
                    return false;

                return response.StatusCode switch
                {
                    HttpStatusCode.ServiceUnavailable => true,
                    HttpStatusCode.GatewayTimeout => true,
                    HttpStatusCode.TooManyRequests => true,
                    _ => false
                };
            }
        };

        var handlers = KiotaClientFactory.CreateDefaultHandlers([retryOptions]).ToList();

        if (defaultPartnerId is not null)
            handlers.Insert(0, new PartnerIdHandler(defaultPartnerId));

        // AutoCancelHandler sits at the outermost position so it sees the final response
        // after all retries are exhausted. Its compensating cancel request flows through the
        // inner handlers (PartnerIdHandler, RetryHandler, etc.) and therefore picks up
        // authentication, partner-id injection, and retry behaviour automatically.
        handlers.Insert(0, new AutoCancelOrderHandler());

        return handlers;
    }

    private static bool IsNoRetryEndpoint(HttpResponseMessage response)
    {
        var path = response.RequestMessage?.RequestUri?.AbsolutePath;
        if (path is null)
            return false;

        // Prefix match handles both fixed paths and parameterised ones
        // (e.g. /Reservation/{id}/cancel).
        return NonIdempotentPaths.All.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
