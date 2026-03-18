import { describe, it, expect } from "vitest";
import { createClient, missingCredentials } from "./fixture";

describe.skipIf(!!missingCredentials)("Product API (integration)", () => {
  it("get returns a result", async () => {
    const client = createClient();

    const result = await client.product.get();

    expect(result).toBeDefined();
    expect(result?.result).toBeDefined();
  });

  it("get returns an array of products", async () => {
    const client = createClient();

    const result = await client.product.get();

    expect(Array.isArray(result?.result)).toBe(true);
  });

  it("get includes pagination metadata", async () => {
    const client = createClient();

    const result = await client.product.get();

    expect(typeof result?.totalCount).toBe("number");
    expect(typeof result?.pageCount).toBe("number");
  });
});
