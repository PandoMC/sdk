import { describe, it, expect } from "vitest";
import { assetsBaseUrl, createClient, missingCredentials, productId, publisherId } from "./fixture";

describe.skipIf(!!missingCredentials || !assetsBaseUrl)("AssetPack API (integration)", () => {
  it("get returns a list of asset packs", async () => {
    const client = createClient();

    const result = await client.assets.assetPack.byPublisherId(publisherId).byProductId(productId).get();

    expect(Array.isArray(result?.result)).toBe(true);
  });

  it("get by id returns the asset pack with its assets", async (context) => {
    const client = createClient();
    const product = client.assets.assetPack.byPublisherId(publisherId).byProductId(productId);

    const packs = await product.get();
    const packId = packs?.result?.[0]?.id;
    if (!packId) context.skip("The configured product has no asset packs.");

    const detail = await product.byAssetPackId(packId!).get();

    expect(detail?.id).toBe(packId);
    expect(Array.isArray(detail?.assets)).toBe(true);
  });
});
