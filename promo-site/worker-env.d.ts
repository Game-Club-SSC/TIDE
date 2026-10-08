// Optional hosting bindings. Keep this aligned with .openai/hosting.json.
// The database accessor validates availability before using DB.
declare namespace Cloudflare {
  interface Env {
    DB?: D1Database;
  }
}
