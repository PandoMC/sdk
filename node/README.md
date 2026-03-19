# MissionControl Client — Node SDK

The Node SDK for the [Mission:Control](https://www.missioncontrol.io) Partner API. Built on top of [Kiota](https://learn.microsoft.com/en-us/openapi/kiota/), it provides a strongly-typed, async client for all partner operations: listing products, managing reservations, and creating orders.

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

### Filtering and pagination

All list endpoints accept an optional `queryParameters` object for filtering and pagination. Every parameter is optional — only set what you need.

**Filter products by publisher, name, and page size:**

```ts
const result = await client.product.get({
  queryParameters: {
    publisherId: ["publisher-id-1", "publisher-id-2"],
    name: "minecraft",
    pageSize: 25,
    pageIndex: 0,
  },
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
| `withAzureAdClientCredentials(clientId, clientSecret, tenantId?)` | Configure authentication                           |
| `withDefaultPartnerId(partnerId)`                                 | Set the `X-Partner-Id` header for every request    |
| `forEnvironment(env)`                                             | Target `Sandbox` or `Production`                   |
| `withBaseUrl(url)`                                                | Override the base URL (e.g. for local development) |
| `withScope(scope)`                                                | Override the OAuth scope                           |
| `build()`                                                         | Return the configured `Client` instance            |

## Resilience

The SDK registers Kiota's default HTTP middleware pipeline, which includes a `RetryHandler` that automatically retries transient failures before surfacing an exception:

| Status code | Meaning             | Behaviour                             |
| ----------- | ------------------- | ------------------------------------- |
| `429`       | Too Many Requests   | Retried after honouring `Retry-After` |
| `503`       | Service Unavailable | Retried with exponential back-off     |
| `504`       | Gateway Timeout     | Retried with exponential back-off     |

The handler retries up to three times by default. If all retries are exhausted without a successful response the last error response is returned to the caller (and mapped to a `ProblemDetails` error where applicable).

No extra configuration is required — retries are active out of the box.

## Error handling

The API returns errors as [RFC 9457 Problem Details](https://www.rfc-editor.org/rfc/rfc9457) JSON objects. The SDK maps these to `ProblemDetails` objects and throws them, which you catch like any other error.

### `ProblemDetails` properties

| Property         | Type                      | Description                                                     |
| ---------------- | ------------------------- | --------------------------------------------------------------- |
| `status`         | `number \| null`          | HTTP status code (e.g. `400`, `500`)                            |
| `title`          | `string \| null`          | Short, human-readable summary of the problem                    |
| `detail`         | `string \| null`          | Longer explanation of this specific occurrence                  |
| `type`           | `string \| null`          | URI identifying the problem type                                |
| `instance`       | `string \| null`          | URI identifying this specific occurrence                        |
| `additionalData` | `Record<string, unknown>` | Any extra fields returned by the API (e.g. `errors`, `traceId`) |

### Status codes

| Status | When it occurs                                                                                      |
| ------ | --------------------------------------------------------------------------------------------------- |
| `400`  | The request failed validation — check `additionalData["errors"]` for per-field messages             |
| `500`  | An unexpected error occurred on the server — `additionalData["traceId"]` can be shared with support |

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

`errors` and `traceId` are not first-class properties on the model — they land in `additionalData` because they are not part of the OpenAPI schema. Cast them as needed:

```ts
import type { ProblemDetails } from "@pandomc/missioncontrol-client";

try {
  const result = await client.product.get({
    queryParameters: {
      productId: ["not-a-guid"],
    },
  });
} catch (ex) {
  if (ex && typeof ex === "object" && "status" in ex) {
    const problem = ex as ProblemDetails;
    console.log(`Failed: ${problem.title}`);

    const errors = problem.additionalData?.["errors"];
    if (errors && typeof errors === "object") {
      for (const [field, messages] of Object.entries(
        errors as Record<string, unknown>,
      )) {
        console.log(`  ${field}: ${messages}`);
      }
    }

    const traceId = problem.additionalData?.["traceId"];
    if (traceId !== undefined) {
      console.log(`  Trace ID: ${traceId}`);
    }
  }
}
```
