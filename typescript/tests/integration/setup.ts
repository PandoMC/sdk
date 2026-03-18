import { existsSync } from "node:fs";
import { resolve } from "node:path";

// Load .env.local from the project root (next to vitest.integration.config.ts).
// This ensures credentials are available via process.env when running integration tests.
// In CI, set the environment variables directly instead of using this file.
const envFile = resolve(import.meta.dirname, "../../.env.local");
if (existsSync(envFile)) {
  process.loadEnvFile(envFile);
}

// Allow self-signed certificates when running against a local development server.
// This is scoped to the integration test process and must never be used in production code.
const baseUrl = process.env["MC_BASE_URL"] ?? "";
if (
  baseUrl.startsWith("http://localhost") ||
  baseUrl.startsWith("https://localhost") ||
  baseUrl.startsWith("http://127.0.0.1") ||
  baseUrl.startsWith("https://127.0.0.1")
) {
  process.env["NODE_TLS_REJECT_UNAUTHORIZED"] = "0";
}
