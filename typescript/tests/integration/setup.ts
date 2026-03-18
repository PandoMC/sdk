import { existsSync } from "node:fs";
import { resolve } from "node:path";

// Load .env.local from the project root (next to vitest.integration.config.ts).
// This ensures credentials are available via process.env when running integration tests.
// In CI, set the environment variables directly instead of using this file.
const envFile = resolve(import.meta.dirname, "../../.env.local");
if (existsSync(envFile)) {
  process.loadEnvFile(envFile);
}
