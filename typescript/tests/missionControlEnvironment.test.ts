import { describe, it, expect } from "vitest";
import {
  MissionControlEnvironment,
  getBaseUrl,
  getScope,
} from "../src/client/missionControlEnvironment";

describe("MissionControlEnvironment", () => {
  describe("enum values", () => {
    it("Sandbox has the expected string value", () => {
      expect(MissionControlEnvironment.Sandbox).toBe("Sandbox");
    });

    it("Production has the expected string value", () => {
      expect(MissionControlEnvironment.Production).toBe("Production");
    });
  });

  describe("getBaseUrl", () => {
    it("returns the sandbox base URL for the Sandbox environment", () => {
      expect(getBaseUrl(MissionControlEnvironment.Sandbox)).toBe(
        "https://api.sandbox.missioncontrol.io",
      );
    });

    it("returns the production base URL for the Production environment", () => {
      expect(getBaseUrl(MissionControlEnvironment.Production)).toBe(
        "https://api.missioncontrol.io",
      );
    });

    it("Sandbox and Production base URLs are different", () => {
      expect(getBaseUrl(MissionControlEnvironment.Sandbox)).not.toBe(
        getBaseUrl(MissionControlEnvironment.Production),
      );
    });
  });

  describe("getScope", () => {
    it("returns the sandbox OAuth scope for the Sandbox environment", () => {
      expect(getScope(MissionControlEnvironment.Sandbox)).toBe(
        "api://38fbfaea-5648-4b51-ac09-d5d90117beff/.default",
      );
    });

    it("returns the production OAuth scope for the Production environment", () => {
      expect(getScope(MissionControlEnvironment.Production)).toBe(
        "api://1b6f40bc-e051-4b7e-987f-47a4a19fa5ef/.default",
      );
    });

    it("Sandbox and Production scopes are different", () => {
      expect(getScope(MissionControlEnvironment.Sandbox)).not.toBe(
        getScope(MissionControlEnvironment.Production),
      );
    });

    it("scope follows the api://<guid>/.default pattern", () => {
      const scopePattern = /^api:\/\/[0-9a-f-]+\/.default$/;
      expect(getScope(MissionControlEnvironment.Sandbox)).toMatch(scopePattern);
      expect(getScope(MissionControlEnvironment.Production)).toMatch(
        scopePattern,
      );
    });
  });
});
