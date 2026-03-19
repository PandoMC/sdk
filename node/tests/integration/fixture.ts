import { ClientBuilder } from "../../src/client/clientBuilder";
import type { Client } from "../../src/client/generated/client";

// Credentials are read from process.env, which Vitest populates from .env.local
// (placed next to vitest.integration.config.ts, never committed).
// Copy .env.local.example to .env.local and fill in your values to run locally.
// In CI, set the environment variables directly.
const REQUIRED_KEYS = [
  "MC_TENANT_ID",
  "MC_CLIENT_ID",
  "MC_CLIENT_SECRET",
  "MC_PARTNER_ID",
  "MC_BASE_URL",
  "MC_SCOPE",
] as const;

const missingKeys = REQUIRED_KEYS.filter((key) => !process.env[key]);
/**
 * When non-empty, one or more required credentials are absent.
 * Tests use this to skip gracefully rather than failing.
 *
 * Copy .env.local.example to .env.local and fill in your values to run integration tests locally.
 * In CI, set the environment variables directly.
 */
export const missingCredentials: string =
  missingKeys.length > 0
    ? `Missing required credentials: ${missingKeys.join(", ")}. ` +
      "Copy .env.local.example to .env.local and fill in your values."
    : "";

export const productId: string = process.env["MC_PRODUCT_ID"] ?? "";
export const regionId: string = process.env["MC_REGION_ID"] ?? "";
export const publisherId: string = process.env["MC_PUBLISHER_ID"] ?? "";

/**
 * Creates a fully configured integration-test client from the loaded credentials.
 * Only call this inside a test that is guarded by {@link missingCredentials}.
 */
export function createClient(): Client {
  return new ClientBuilder()
    .withAzureAdClientCredentials(
      process.env["MC_CLIENT_ID"]!,
      process.env["MC_CLIENT_SECRET"]!,
      process.env["MC_TENANT_ID"],
    )
    .withDefaultPartnerId(process.env["MC_PARTNER_ID"]!)
    .withBaseUrl(process.env["MC_BASE_URL"]!)
    .withScope(process.env["MC_SCOPE"]!)
    .build();
}
