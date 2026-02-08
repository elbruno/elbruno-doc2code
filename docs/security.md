# Security Considerations

## Authentication

The solution uses a shared API key to protect all `/api/*` and SignalR
endpoints across both the ApiService and the SettingsService.

### How it works

- A shared middleware (`ApiKeyMiddleware` in ServiceDefaults) validates the
  `X-Api-Key` header on every incoming request.
- For SignalR / WebSocket upgrades (where browsers cannot set custom headers)
  the middleware also accepts the key as an `access_token` query parameter.
- Health-check endpoints (`/health`, `/alive`) are always excluded from
  validation.

### Configuration

| Method | Details |
|--------|---------|
| **Aspire parameter** (recommended) | Declared as `builder.AddParameter("apikey", secret: true)` in the AppHost. Aspire prompts for the value on first run and stores it in .NET user secrets. |
| **Environment variable** | Set the `ApiKey` environment variable on each service process. |
| **appsettings.json** | Add `"ApiKey": "<your-key>"` to the configuration file (not recommended for production). |

### Opt-in design

When no key is configured (empty or null), the middleware is a transparent
pass-through. This preserves the zero-config local development experience — no
key is required for purely local use. **Always set a key when the services are
accessible over a network.**

### Outgoing calls

The Blazor frontend and inter-service HTTP clients use a
`ApiKeyDelegatingHandler` that automatically attaches the `X-Api-Key` header to
every outgoing request. The SignalR client sends the key both as a header and
via the `access_token` query parameter.

## Token Handling

- GitHub personal access tokens are stored in `Doc2CodeConfig.SourceControl.Token`
  and persisted in the SettingsService JSON file. The file lives inside the
  container's data volume and is not exposed externally.
- Tokens are transmitted over internal Aspire service-discovery URLs
  (`https+http://settingsservice`), which use TLS in production.
- **Never commit** `settings.json` or `.env` files containing tokens to source control.
  The `.gitignore` already excludes `*.env`.

## Ollama Network Exposure

- By default the Ollama container binds to `localhost` through Docker port mapping.
  Aspire handles the wiring internally.
- If you expose Ollama on a public IP, add authentication at the reverse-proxy layer
  because Ollama itself has no built-in auth.

## SignalR Hubs

- `AgentLogHub` on the ApiService streams pipeline logs to connected Blazor clients.
- The hub endpoints are protected by the shared API-key middleware (see
  **Authentication** above). The key is sent as an `X-Api-Key` header for
  regular HTTP calls and as an `access_token` query parameter for WebSocket
  upgrade requests.

## Input Validation

- Uploaded requirements documents are read as plain text streams by
  `TextDocumentIngester`. There is no file-type enforcement beyond the extension
  check in `Supports()`.
- The SettingsService deserialises incoming JSON with
  `System.Text.Json`; unknown properties are ignored. No dynamic code execution
  occurs on user-supplied data.

## Known Gaps

| Area | Gap | Mitigation Suggestion |
|------|-----|----------------------|
| File uploads | No size limit enforced | Add `RequestSizeLimit` on the upload endpoint |
| Settings file | Stored as plain JSON | Encrypt sensitive fields at rest |
