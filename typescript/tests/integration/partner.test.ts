import { describe, it, expect } from "vitest";
import { createClient, missingCredentials } from "./fixture";

describe.skipIf(!!missingCredentials)("Partner API (integration)", () => {
  it("get returns a result", async () => {
    const client = createClient();

    const result = await client.partner.get();

    expect(result).toBeDefined();
    expect(result?.result).toBeDefined();
  });

  it("get returns an array of partners", async () => {
    const client = createClient();

    const result = await client.partner.get();

    expect(Array.isArray(result?.result)).toBe(true);
  });

  it("get includes pagination metadata", async () => {
    const client = createClient();

    const result = await client.partner.get();

    expect(result?.totalCount).toBeDefined();
    expect(typeof result?.totalCount).toBe("number");
    expect(result?.pageCount).toBeDefined();
    expect(typeof result?.pageCount).toBe("number");
  });
});
