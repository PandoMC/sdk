using Microsoft.Extensions.Configuration;

namespace MissionControl.Client.IntegrationTests;

/// <summary>
/// Provides a configured <see cref="Generated.Client"/> for integration tests.
/// Use as an <c>IClassFixture&lt;IntegrationTestFixture&gt;</c> on every integration test class.
/// </summary>
/// <remarks>
/// Credentials are loaded first from <c>secrets.json</c> (placed next to the test assembly,
/// never committed — copy <c>secrets.json.example</c> to get started), then from environment
/// variables which take precedence. This means CI/CD sets env vars as usual, while local runs
/// use the file.
///
/// Required keys (in secrets.json or as environment variables):
///   MC_TENANT_ID      – Azure AD tenant ID
///   MC_CLIENT_ID      – Application (client) ID
///   MC_CLIENT_SECRET  – Client secret
///   MC_PARTNER_ID     – Default partner ID sent with every request
///   MC_BASE_URL       – Fully-qualified base URL, e.g. https://api.sandbox.missioncontrol.io or http://localhost:5000
///   MC_SCOPE          – OAuth scope URI, e.g. api://&lt;app-id&gt;/.default
///   MC_PRODUCT_ID     – Product GUID used in reservation write tests
///   MC_REGION_ID      – Region GUID used in reservation write tests
/// </remarks>
public sealed class IntegrationTestFixture
{
    private static readonly IConfiguration Config = new ConfigurationBuilder()
        .AddUserSecrets<IntegrationTestFixture>()
        .AddEnvironmentVariables()
        .Build();

    private readonly Generated.Client? _client;
    private readonly string? _missing;

    public Guid ProductId { get; }
    public Guid RegionId { get; }

    public IntegrationTestFixture()
    {
        var tenantId = Config["MC_TENANT_ID"];
        var clientId = Config["MC_CLIENT_ID"];
        var clientSecret = Config["MC_CLIENT_SECRET"];
        var baseUrl = Config["MC_BASE_URL"];
        var scope = Config["MC_SCOPE"];
        var partnerId = Config["MC_PARTNER_ID"];
        var productId = Config["MC_PRODUCT_ID"];
        var regionId = Config["MC_REGION_ID"];

        if (string.IsNullOrWhiteSpace(tenantId) ||
            string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret) ||
            string.IsNullOrWhiteSpace(partnerId) ||
            string.IsNullOrWhiteSpace(baseUrl) ||
            string.IsNullOrWhiteSpace(scope) ||
            string.IsNullOrWhiteSpace(productId) ||
            string.IsNullOrWhiteSpace(regionId))
        {
            _missing =
                "Integration credentials are not configured. " +
                "The following secrets are required: MC_TENANT_ID, MC_CLIENT_ID, MC_CLIENT_SECRET, MC_PARTNER_ID, MC_BASE_URL, MC_SCOPE, MC_PRODUCT_ID, MC_REGION_ID.";
            return;
        }

        ProductId = Guid.Parse(productId);
        RegionId = Guid.Parse(regionId);

        _client = new ClientBuilder()
            .WithAzureAdClientCredentials(clientId, clientSecret, tenantId)
            .WithDefaultPartnerId(partnerId)
            .WithBaseUrl(baseUrl)
            .WithScope(scope)
            .Build();
    }

    /// <summary>
    /// Returns the configured client. Throws <see cref="InvalidOperationException"/> — and
    /// therefore fails the test — when credentials have not been configured.
    /// </summary>
    public Generated.Client GetClient()
    {
        if (_missing is not null)
            throw new InvalidOperationException(_missing);

        return _client!;
    }
}
