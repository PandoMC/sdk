using Azure.Identity;
using Microsoft.Kiota.Authentication.Azure;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using System.Net;

namespace MissionControl.Client;

/// <summary>
/// Fluent builder for creating a configured <see cref="Generated.Client"/> instance.
/// </summary>
public sealed class ClientBuilder
{
    private string _baseUrl = MissionControlEnvironment.Production.BaseUrl();
    private string _scope = MissionControlEnvironment.Production.Scope();
    private string _tenantId = "13d084c1-d072-41f2-878b-c45ca721c9f6";
    private string? _defaultPartnerId;

    /// <summary>
    /// Sets the default <c>X-Partner-Id</c> header value sent with every request.
    /// <para>
    /// Most partners operate under a single partner identity, so setting this once on the
    /// builder means you never have to specify it per-request. When a request explicitly sets
    /// the header (via <c>q.Headers.Add("X-Partner-Id", "…")</c>) that value takes precedence
    /// over the default configured here.
    /// </para>
    /// </summary>
    /// <param name="partnerId">
    /// The partner ID shown on the client credentials page in the portal.
    /// </param>
    /// <returns>The current builder instance for method chaining.</returns>
    public ClientBuilder WithDefaultPartnerId(string partnerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partnerId);
        _defaultPartnerId = partnerId;
        return this;
    }

    /// <summary>
    /// Overrides the base URL used for all API requests.
    /// Useful for pointing at a local development server or a custom endpoint.
    /// When combined with <see cref="ForEnvironment"/>, the last call wins.
    /// </summary>
    /// <param name="baseUrl">The base URL, e.g. <c>http://localhost:5000</c>.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public ClientBuilder WithBaseUrl(string baseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        _baseUrl = baseUrl;
        return this;
    }

    /// <summary>
    /// Overrides the OAuth scope used when acquiring tokens.
    /// Useful when running against a local or custom environment whose app registration
    /// differs from the well-known environments.
    /// When combined with <see cref="ForEnvironment"/>, the last call wins.
    /// </summary>
    /// <param name="scope">The full scope URI, e.g. <c>api://&lt;app-id&gt;/.default</c>.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public ClientBuilder WithScope(string scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        _scope = scope;
        return this;
    }

    /// <summary>
    /// Targets a well-known MissionControl environment. Sets the matching base URL and API scope together.
    /// Defaults to <see cref="MissionControlEnvironment.Production"/> when not called.
    /// </summary>
    /// <param name="environment">The target environment.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public ClientBuilder ForEnvironment(MissionControlEnvironment environment)
    {
        _baseUrl = environment.BaseUrl();
        _scope = environment.Scope();
        return this;
    }

    /// <summary>
    /// Configures the client to authenticate using Azure AD client credentials (client secret flow).
    /// The API scope is derived from the configured environment unless overridden by
    /// <see cref="WithBaseUrl"/>.
    /// </summary>
    /// <param name="tenantId">The Azure AD tenant ID.</param>
    /// <param name="clientId">The application (client) ID of the app registration.</param>
    /// <param name="clientSecret">The client secret of the app registration.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public ClientBuilder WithAzureAdClientCredentials(
        string clientId,
        string clientSecret,
        string? tenantId = null)
    {
        // Auth provider is built in Build() so the scope resolved by ForEnvironment() is always used.
        _clientId = clientId;
        _clientSecret = clientSecret;
        _tenantId = tenantId ?? _tenantId;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured <see cref="Generated.Client"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when credentials have not been configured via <see cref="WithAzureAdClientCredentials"/>.
    /// </exception>
    public Generated.Client Build()
    {
        if (_tenantId == null || _clientId == null || _clientSecret == null)
        {
            throw new InvalidOperationException(
                "No credentials configured. " +
                $"Call {nameof(WithAzureAdClientCredentials)}() before calling {nameof(Build)}().");
        }

        var credential = new ClientSecretCredential(_tenantId, _clientId, _clientSecret);
        var authProvider = new AzureIdentityAuthenticationProvider(credential, scopes: _scope);

        var handlers = CreateHandlers(_defaultPartnerId);
        var httpClient = KiotaClientFactory.Create(handlers);

        var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient)
        {
            BaseUrl = _baseUrl
        };

        return new Generated.Client(adapter);
    }

    /// <summary>
    /// Endpoints that must never be retried because they are not idempotent.
    /// A retry could cause duplicate orders, reservations, or cancellations.
    /// </summary>
    private static readonly HashSet<string> _noRetryPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/Order/createOrder",
        "/Order/claimReservation",
        "/Order/reportOrder",
    };

    private static bool IsNoRetryEndpoint(HttpResponseMessage response)
    {
        var path = response.RequestMessage?.RequestUri?.AbsolutePath;
        if (path is null)
            return false;

        // Exact match for fixed paths, or prefix match for parameterised ones
        // (e.g. /Reservation/{id}/cancel).
        return _noRetryPaths.Any(noRetryPath => path.StartsWith(noRetryPath, StringComparison.OrdinalIgnoreCase));
    }

    private static List<DelegatingHandler> CreateHandlers(string? defaultPartnerId)
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

    // Stored until Build() so ForEnvironment() called after WithAzureAdClientCredentials()
    // still picks up the correct scope.
    private string? _clientId;
    private string? _clientSecret;
}
