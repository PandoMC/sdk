import { describe, it, expect } from "vitest";
import { MissionControlHeaders } from "../src/client/missionControlHeaders.js";

describe("MissionControlHeaders", () => {
  it('PartnerId equals "X-Partner-Id"', () => {
    expect(MissionControlHeaders.PartnerId).toBe("X-Partner-Id");
  });

  it("is a non-empty string", () => {
    expect(typeof MissionControlHeaders.PartnerId).toBe("string");
    expect(MissionControlHeaders.PartnerId.length).toBeGreaterThan(0);
  });

  it("uses the standard HTTP header casing convention", () => {
    // Header names should be in Train-Case (e.g. X-Partner-Id)
    expect(MissionControlHeaders.PartnerId).toMatch(
      /^[A-Z][a-zA-Z0-9]*(-[A-Z][a-zA-Z0-9]*)*$/,
    );
  });
});
