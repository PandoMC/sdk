# MissionControl.Client — .NET SDK

The .NET SDK for the [Mission:Control](https://www.missioncontrol.io) Partner API. Built on top of [Kiota](https://learn.microsoft.com/en-us/openapi/kiota/), it provides a strongly-typed, async client for all partner operations: listing products, managing reservations, and creating orders.

## Requirements

- .NET 8 or later

## Installation

```
dotnet add package MissionControl.Client
```

## Getting started

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
