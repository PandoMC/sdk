# MissionControl.Client — .NET SDK

The .NET SDK for the [Mission:Control](https://www.missioncontrol.io) Partner API. Built on top of [Kiota](https://learn.microsoft.com/en-us/openapi/kiota/), it provides a strongly-typed, async client for all partner operations: listing products, managing reservations, and creating orders.

## Requirements

- .NET 8 or later

## Installation

```
dotnet add package MissionControl.Client
```

## Getting started quickly

### Creating a client

Credentials are obtained from the **Client Credentials** page in the Mission:Control portal.

```csharp
using MissionControl.Client;

var client = new ClientBuilder()
    .WithAzureAdClientCredentials(
        clientId:     "your-client-id",
        clientSecret: "your-client-secret")
    .WithDefaultPartnerId("your-partner-id")
    .Build();
```

By default the client targets **Production**. To use the **Sandbox** environment during development:

```csharp
var client = new ClientBuilder()
    .ForEnvironment(MissionControlEnvironment.Sandbox)
    .WithAzureAdClientCredentials(
        clientId:     "your-client-id",
        clientSecret: "your-client-secret")
    .WithDefaultPartnerId("your-partner-id")
    .Build();
```

### Dependency injection (ASP.NET Core)

Register the client as a singleton so the underlying `HttpClient` and token cache are shared across the application lifetime.

```csharp
// Program.cs / Startup.cs
builder.Services.AddSingleton(_ =>
    new ClientBuilder()
        .ForEnvironment(MissionControlEnvironment.Production)
        .WithAzureAdClientCredentials(
            clientId:     builder.Configuration["MissionControl:ClientId"]!,
            clientSecret: builder.Configuration["MissionControl:ClientSecret"]!)
        .WithDefaultPartnerId(builder.Configuration["MissionControl:PartnerId"]!)
        .Build());
```

Then inject `MissionControl.Client.Generated.Client` wherever you need it:

```csharp
public class ProductService(MissionControl.Client.Generated.Client mcClient)
{
    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default)
    {
        var result = await mcClient.Product.GetAsync(cancellationToken: ct);
        return result?.Result ?? [];
    }
}
```

## Usage examples

### List products

```csharp
var result = await client.Product.GetAsync();

foreach (var product in result!.Result!)
    Console.WriteLine($"{product.Id}  {product.Name}");
```

### Check stock

```csharp
var response = await client.Product.HasStock.PostAsync(new HasStockRequest
{
    ProductId = productId,
    RegionId  = regionId,
});

Console.WriteLine(response!.HasStock == true ? "In stock" : "Out of stock");
```

### Reserve → claim (place an order)

```csharp
// 1. Create a reservation — holds the key briefly while the customer checks out.
var reservation = await client.Reservation.PostAsync(new CreateReservationRequest
{
    ProductId = productId,
    RegionId  = regionId,
});

// 2. Claim the reservation to finalise the order and receive the key.
var order = await client.Order.ClaimReservation[reservation!.Id!.Value].PostAsync(
    new ClaimReservationRequest
    {
        RequestId        = Guid.NewGuid(),
        PartnerReference = "your-internal-order-ref",
        InvoiceDate      = DateTimeOffset.UtcNow,
        SalesChannel     = [],   // empty if selling directly to consumer
        EndConsumer = new Consumer
        {
            IpAddress = "1.2.3.4",
            Language  = "en-US",
            Location  = new Location { CountryCode = "NL" },
        },
        SalesPrice = new SalesPriceRequest
        {
            PriceIncludingVat = new Money { Value = 9.99, CurrencyCode = "EUR" },
            VatDetail         = new VatDetail { Rate = 21 },
        },
    });

Console.WriteLine($"Order {order!.Id} placed.");
```

### Cancel a reservation

```csharp
await client.Reservation[reservation!.Id!.Value].Cancel.PostAsync();
```

### Filtering and pagination

All list endpoints accept an optional query parameter lambda for filtering and pagination. Every parameter is optional — only set what you need.

**Filter products by publisher, name, and page size:**

```csharp
var result = await client.Product.GetAsync(q =>
{
    q.QueryParameters.PublisherId = [publisherId];
    q.QueryParameters.Name        = "minecraft";
    q.QueryParameters.PageSize    = 25;
    q.QueryParameters.PageIndex   = 0;
});
```

## Configuration reference

| Key            | Required | Description                                                 |
| -------------- | -------- | ----------------------------------------------------------- |
| `clientId`     | ✓        | Application (client) ID from the portal                     |
| `clientSecret` | ✓        | Client secret from the portal                               |
| `partnerId`    | ✓        | Partner ID sent as `X-Partner-Id` with every request        |
| `environment`  | —        | `Sandbox` or `Production` (default: `Production`)           |
| `tenantId`     | —        | Azure AD tenant ID (defaults to the Mission:Control tenant) |

### `ClientBuilder` methods

| Method                                                            | Description                                        |
| ----------------------------------------------------------------- | -------------------------------------------------- |
| `WithAzureAdClientCredentials(clientId, clientSecret, tenantId?)` | Configure authentication                           |
| `WithDefaultPartnerId(partnerId)`                                 | Set the `X-Partner-Id` header for every request    |
| `ForEnvironment(env)`                                             | Target `Sandbox` or `Production`                   |
| `WithBaseUrl(url)`                                                | Override the base URL (e.g. for local development) |
| `WithScope(scope)`                                                | Override the OAuth scope                           |
| `Build()`                                                         | Return the configured `Client` instance            |

## Resilience

The SDK registers Kiota's default HTTP middleware pipeline, which includes a `RetryHandler` that automatically retries transient failures before surfacing an exception:

| Status code | Meaning             | Behaviour                             |
| ----------- | ------------------- | ------------------------------------- |
| `429`       | Too Many Requests   | Retried after honouring `Retry-After` |
| `503`       | Service Unavailable | Retried with exponential back-off     |
| `504`       | Gateway Timeout     | Retried with exponential back-off     |

The handler retries up to three times by default. If all retries are exhausted without a successful response the last error response is returned to the caller (and mapped to a `ProblemDetails` exception where applicable).

No extra configuration is required — retries are active out of the box.

## Error handling

The API returns errors as [RFC 9457 Problem Details](https://www.rfc-editor.org/rfc/rfc9457) JSON objects. The SDK maps these to `ProblemDetails` exceptions, which you catch like any other exception.

### `ProblemDetails` properties

| Property         | Type                         | Description                                                     |
| ---------------- | ---------------------------- | --------------------------------------------------------------- |
| `Status`         | `int?`                       | HTTP status code (e.g. `400`, `500`)                            |
| `Title`          | `string?`                    | Short, human-readable summary of the problem                    |
| `Detail`         | `string?`                    | Longer explanation of this specific occurrence                  |
| `Type`           | `string?`                    | URI identifying the problem type                                |
| `Instance`       | `string?`                    | URI identifying this specific occurrence                        |
| `AdditionalData` | `IDictionary<string,object>` | Any extra fields returned by the API (e.g. `errors`, `traceId`) |

### Status codes

| Status | When it occurs                                                                                      |
| ------ | --------------------------------------------------------------------------------------------------- |
| `400`  | The request failed validation — check `AdditionalData["errors"]` for per-field messages             |
| `500`  | An unexpected error occurred on the server — `AdditionalData["traceId"]` can be shared with support |

### Handling a validation error (400)

A 400 response from the API looks like:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "ProductId": ["The value 'foobar' is not valid."]
  },
  "traceId": "00-90f2dadbc2e77ae87cbe3b8a25fe0689-dbff6f5f82d028d2-00"
}
```

`errors` and `traceId` are not first-class properties on the model — they land in `AdditionalData` because they are not part of the OpenAPI schema. Cast them as needed:

```csharp
using MissionControl.Client.Generated.Models;

try
{
    var result = await client.Product.GetAsync(q =>
    {
        q.QueryParameters.ProductId = [Guid.Parse("not-a-guid")];
    });
}
catch (ProblemDetails ex)
{
    Console.WriteLine($"Failed: {ex.Title}");

    if (ex.AdditionalData.TryGetValue("errors", out var raw) &&
        raw is Dictionary<string, object> errors)
    {
        foreach (var (field, messages) in errors)
            Console.WriteLine($"  {field}: {messages}");
    }

    if (ex.AdditionalData.TryGetValue("traceId", out var traceId))
        Console.WriteLine($"  Trace ID: {traceId}");
}
```
