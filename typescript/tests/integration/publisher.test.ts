import { describe, it, expect } from "vitest";
import { createClient, missingCredentials } from "./fixture";

describe.skipIf(!!missingCredentials)("Publisher API (integration)", () => {
  it("get returns a result", async () => {
    const client = createClient();

    const result = await client.publisher.get();

    expect(result).toBeDefined();
    expect(result?.result).toBeDefined();
  });

  it("get returns an array of publishers", async () => {
    const client = createClient();

    const result = await client.publisher.get();

    expect(Array.isArray(result?.result)).toBe(true);
  });

  it("get includes pagination metadata", async () => {
    const client = createClient();

    const result = await client.publisher.get();

    expect(typeof result?.totalCount).toBe("number");
    expect(typeof result?.pageCount).toBe("number");
  });
});
