import { describe, it, expect } from "vitest";
import { createClient, missingCredentials } from "./fixture";

describe.skipIf(!!missingCredentials)("PriceList API (integration)", () => {
  it("get returns a price list for the first available product", async () => {
    const client = createClient();

    // Discover the first available product so no extra env var is needed.
    const products = await client.product.get();
    const product = products?.result?.[0];

    if (!product) {
      console.log(
        "No products found in the configured environment — skipping.",
      );
      return;
    }

    const result = await client.priceList.get({
      queryParameters: {
        publisherId: product.publisherId ?? undefined,
      },
    });

    expect(result).toBeDefined();
  });
});
