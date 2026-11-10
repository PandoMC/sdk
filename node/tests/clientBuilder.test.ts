import { describe, it, expect } from "vitest";
import { ClientBuilder } from "../src/client/clientBuilder";
import { MissionControlEnvironment } from "../src/client/missionControlEnvironment";

describe("ClientBuilder", () => {
  describe("withDefaultPartnerId", () => {
    it("throws when partnerId is an empty string", () => {
      expect(() => new ClientBuilder().withDefaultPartnerId("")).toThrow(
        "partnerId must not be null or whitespace.",
      );
    });

    it("throws when partnerId is whitespace only", () => {
      expect(() => new ClientBuilder().withDefaultPartnerId("   ")).toThrow(
        "partnerId must not be null or whitespace.",
      );
    });

    it("returns the builder instance for fluent chaining", () => {
      const builder = new ClientBuilder();
      expect(builder.withDefaultPartnerId("partner-123")).toBe(builder);
    });
  });

  describe("withBaseUrl", () => {
    it("throws when baseUrl is an empty string", () => {
      expect(() => new ClientBuilder().withBaseUrl("")).toThrow(
        "baseUrl must not be null or whitespace.",
      );
    });

    it("throws when baseUrl is whitespace only", () => {
      expect(() => new ClientBuilder().withBaseUrl("  ")).toThrow(
        "baseUrl must not be null or whitespace.",
      );
    });

    it("returns the builder instance for fluent chaining", () => {
      const builder = new ClientBuilder();
      expect(builder.withBaseUrl("http://localhost:5000")).toBe(builder);
    });
  });

  describe("withScope", () => {
    it("throws when scope is an empty string", () => {
      expect(() => new ClientBuilder().withScope("")).toThrow(
        "scope must not be null or whitespace.",
      );
    });

    it("throws when scope is whitespace only", () => {
      expect(() => new ClientBuilder().withScope("  ")).toThrow(
        "scope must not be null or whitespace.",
      );
    });

    it("returns the builder instance for fluent chaining", () => {
      const builder = new ClientBuilder();
      expect(builder.withScope("api://app-id/.default")).toBe(builder);
    });
  });

  describe("forEnvironment", () => {
    it("returns the builder instance for the Sandbox environment", () => {
      const builder = new ClientBuilder();
      expect(builder.forEnvironment(MissionControlEnvironment.Sandbox)).toBe(
        builder,
      );
    });

    it("returns the builder instance for the Production environment", () => {
      const builder = new ClientBuilder();
      expect(builder.forEnvironment(MissionControlEnvironment.Production)).toBe(
        builder,
      );
    });

    it("can be chained after withAzureAdClientCredentials", () => {
      const builder = new ClientBuilder();
      const result = builder
        .withAzureAdClientCredentials("id", "secret")
        .forEnvironment(MissionControlEnvironment.Sandbox);
      expect(result).toBe(builder);
    });
  });

  describe("withAzureAdClientCredentials", () => {
    it("returns the builder instance for fluent chaining", () => {
      const builder = new ClientBuilder();
      expect(
        builder.withAzureAdClientCredentials("client-id", "client-secret"),
      ).toBe(builder);
    });

    it("returns the builder instance when a custom tenantId is provided", () => {
      const builder = new ClientBuilder();
      expect(
        builder.withAzureAdClientCredentials(
          "client-id",
          "client-secret",
          "tenant-id",
        ),
      ).toBe(builder);
    });
  });

  describe("build", () => {
    it("throws when no credentials have been configured", () => {
      expect(() => new ClientBuilder().build()).toThrow(
        "No credentials configured. Call withAzureAdClientCredentials() before calling build().",
      );
    });

    it("throws even after forEnvironment is called without credentials", () => {
      expect(() =>
        new ClientBuilder()
          .forEnvironment(MissionControlEnvironment.Sandbox)
          .build(),
      ).toThrow("No credentials configured.");
    });

    it("returns a client when credentials are configured", () => {
      const client = new ClientBuilder()
        .withAzureAdClientCredentials("fake-client-id", "fake-client-secret")
        .build();
      expect(client).toBeDefined();
    });

    it("returns a client for the Sandbox environment", () => {
      const client = new ClientBuilder()
        .forEnvironment(MissionControlEnvironment.Sandbox)
        .withAzureAdClientCredentials("fake-client-id", "fake-client-secret")
        .build();
      expect(client).toBeDefined();
    });

    it("returns a client when a default partner ID is set", () => {
      const client = new ClientBuilder()
        .withDefaultPartnerId("partner-123")
        .withAzureAdClientCredentials("fake-client-id", "fake-client-secret")
        .build();
      expect(client).toBeDefined();
    });

    it("returns a client when a custom base URL and scope override are set", () => {
      const client = new ClientBuilder()
        .withBaseUrl("http://localhost:5000")
        .withScope("api://local-app-id/.default")
        .withAzureAdClientCredentials("fake-client-id", "fake-client-secret")
        .build();
      expect(client).toBeDefined();
    });
  });

  describe("assets", () => {
    const publisherId = "3c9e8b7a-1d2f-4e5a-9b8c-7d6e5f4a3b21";
    const productId = "80bd6d7b-cbe4-4279-af5f-fdaa16998e50";
    const assetPacksUrl = (builder: ClientBuilder) =>
      builder
        .withAzureAdClientCredentials("fake-client-id", "fake-client-secret")
        .build()
        .assets.assetPack.byPublisherId(publisherId)
        .byProductId(productId)
        .toGetRequestInformation().URL;

    it("uses the assets base URL of the environment", () => {
      const builder = new ClientBuilder().forEnvironment(MissionControlEnvironment.Sandbox);
      expect(assetPacksUrl(builder)).toBe(
        `https://api.sandbox.missioncontrol.io/assets/partnerApi/v1/AssetPack/${publisherId}/${productId}`,
      );
    });

    it("uses an overridden assets base URL", () => {
      const builder = new ClientBuilder().withAssetsBaseUrl("http://localhost:5001/partnerApi/v1");
      expect(assetPacksUrl(builder)).toBe(
        `http://localhost:5001/partnerApi/v1/AssetPack/${publisherId}/${productId}`,
      );
    });

    it("keeps the core API on its own base URL", () => {
      const client = new ClientBuilder()
        .forEnvironment(MissionControlEnvironment.Sandbox)
        .withAzureAdClientCredentials("fake-client-id", "fake-client-secret")
        .build();
      expect(client.product.toGetRequestInformation().URL).toBe(
        "https://api.sandbox.missioncontrol.io/partnerApi/v2/Product",
      );
    });

    it("throws when assetsBaseUrl is whitespace only", () => {
      expect(() => new ClientBuilder().withAssetsBaseUrl("  ")).toThrow(
        "assetsBaseUrl must not be null or whitespace.",
      );
    });
  });
});
