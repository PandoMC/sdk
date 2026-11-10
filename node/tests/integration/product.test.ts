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

  it("redemptionInstructions returns a list", async (context) => {
    const client = createClient();

    const products = await client.product.get();
    const productId = products?.result?.[0]?.id;
    if (!productId) context.skip("No products found in the configured environment.");

    const instructions = await client.product.byProductId(productId!).redemptionInstructions.get();

    // An empty list is valid: not every product has instructions assigned.
    expect(Array.isArray(instructions)).toBe(true);
    for (const instruction of instructions ?? []) expect(instruction.languageCode).toBeTruthy();
  });
});
