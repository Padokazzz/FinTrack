import { spawnSync } from "node:child_process";
import { existsSync } from "node:fs";
import { fileURLToPath } from "node:url";

const candidates = [".env.local", ".env"].map((name) =>
  fileURLToPath(new URL(`../../${name}`, import.meta.url)),
);
const source = candidates.find(existsSync);
if (source) process.loadEnvFile(source);
const runtime = fileURLToPath(new URL("../../.env.fintrack.local", import.meta.url));
if (existsSync(runtime)) process.loadEnvFile(runtime);
if (process.env.FINTRACK_DATABASE_URL) process.env.DATABASE_URL = process.env.FINTRACK_DATABASE_URL;

const [command, ...args] = process.argv.slice(2);
if (!command || !process.env.DATABASE_URL) {
  console.error("Usage: node deploy/with-neon-env.mjs <command> [args]. Set DATABASE_URL or run neon deploy in the workspace.");
  process.exit(1);
}

const result = spawnSync(command, args, { stdio: "inherit" });
if (result.error) console.error(result.error.message);
process.exit(result.status ?? 1);
