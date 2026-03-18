# MissionControl Client — TypeScript SDK

The TypeScript SDK for the [Mission:Control](https://www.missioncontrol.io) Partner API. Built on top of [Kiota](https://learn.microsoft.com/en-us/openapi/kiota/), it provides a strongly-typed, async client for all partner operations: listing products, managing reservations, and creating orders.

## Requirements

- Node.js 18 or later

## Installation

```
npm install @pandomc/missioncontrol-client
```

## Getting started

### Creating a client

Credentials are obtained from the **Client Credentials** page in the Mission:Control portal.

```ts
import { ClientBuilder } from "@pandomc/missioncontrol-client";

const client = new ClientBuilder()
  .withAzureAdClientCredentials("your-client-id", "your-client-secret")
  .withDefaultPartnerId("your-partner-id")
  .build();
```

By default the client targets **Production**. To use the **Sandbox** environment during development:

```ts
import {
  ClientBuilder,
  MissionControlEnvironment,
} from "@pandomc/missioncontrol-client";

const client = new ClientBuilder()
  .forEnvironment(MissionControlEnvironment.Sandbox)
  .withAzureAdClientCredentials("your-client-id", "your-client-secret")
  .withDefaultPartnerId("your-partner-id")
  .build();
```

## Usage examples

### List products

```ts
const result = await client.product.get();

for (const product of result?.result ?? []) {
  console.log(product.id, product.name);
}
```

### Check stock

```ts
const response = await client.product.hasStock.post({
  productId: productId,
  regionId: regionId,
});

console.log(response?.hasStock ? "In stock" : "Out of stock");
```

### Reserve → claim (place an order)

```ts
// 1. Create a reservation — holds the key briefly while the customer checks out.
const reservation = await client.reservation.post({
  productId: productId,
  regionId: regionId,
});

// 2. Claim the reservation to finalise the order and receive the key.
const order = await client.order.claimReservation
  .byReservationId(reservation!.id!)
  .post({
    requestId: crypto.randomUUID(),
    partnerReference: "your-internal-order-ref",
    invoiceDate: new Date().toISOString(),
    salesChannel: [],
    endConsumer: {
      ipAddress: "1.2.3.4",
      language: "en-US",
      location: { countryCode: "NL" },
    },
    salesPrice: {
      priceIncludingVat: { value: 9.99, currencyCode: "EUR" },
      vatDetail: { rate: 21 },
    },
  });

console.log(`Order ${order!.id} placed.`);
```

### Cancel a reservation

```ts
await client.reservation.byReservationId(reservation!.id!).cancel.post();
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
| `withAzureAdClientCredentials(clientId, clientSecret, tenantId?)` | Configure authentication                           |
| `withDefaultPartnerId(partnerId)`                                 | Set the `X-Partner-Id` header for every request    |
| `forEnvironment(env)`                                             | Target `Sandbox` or `Production`                   |
| `withBaseUrl(url)`                                                | Override the base URL (e.g. for local development) |
| `withScope(scope)`                                                | Override the OAuth scope                           |
| `build()`                                                         | Return the configured `Client` instance            |
