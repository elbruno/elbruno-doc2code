# Security Considerations

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
- There is currently **no authentication or authorisation** on the hub endpoints.
  This is acceptable for local development but should be addressed before any
  public deployment (e.g., require a JWT bearer token on the hub connection).

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
| SignalR auth | No token required | Add JWT bearer middleware on hub routes |
| File uploads | No size limit enforced | Add `RequestSizeLimit` on the upload endpoint |
| Settings file | Stored as plain JSON | Encrypt sensitive fields at rest |
