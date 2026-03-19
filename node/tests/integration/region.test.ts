import { describe, it, expect } from "vitest";
import { createClient, missingCredentials, publisherId } from "./fixture";

describe.skipIf(!!missingCredentials)("Region API (integration)", () => {
  it("get returns a result", async () => {
    const client = createClient();

    const result = await client.region.get({
      queryParameters: { publisherId },
    });

    expect(result).toBeDefined();
    expect(result?.result).toBeDefined();
  });

  it("get returns an array of regions", async () => {
    const client = createClient();

    const result = await client.region.get({
      queryParameters: { publisherId },
    });

    expect(Array.isArray(result?.result)).toBe(true);
  });

  it("get includes pagination metadata", async () => {
    const client = createClient();

    const result = await client.region.get({
      queryParameters: { publisherId },
    });

    expect(typeof result?.totalCount).toBe("number");
    expect(typeof result?.pageCount).toBe("number");
  });
});
