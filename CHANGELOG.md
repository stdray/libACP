# Changelog

All notable changes to this project will be documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] — 2026-05-17

### Added
- Initial release of the .NET implementation of the
  [Agent Client Protocol (ACP)](https://agentclientprotocol.com), protocol version `1`.
- Multi-target `net8.0` and `net10.0`.
- JSON-RPC 2.0 layer over newline-delimited JSON (`NdJsonStream`) with concurrent
  request correlation, parse-error reporting, and a 16 MiB max-line guard.
- Full stable schema DTOs (immutable `record`s) for:
  - `initialize`, `authenticate`
  - `session/{new,load,resume,list,close,prompt,cancel,update,set_mode,set_config_option,request_permission}`
  - `fs/{read_text_file,write_text_file}`
  - `terminal/{create,output,release,wait_for_exit,kill}`
- Discriminated-union JSON converters for `ContentBlock`, `SessionUpdate`,
  `RequestPermissionOutcome`, `ToolCallContent`, `EmbeddedResourceResource`,
  `McpServer`, and `RequestId`.
- `IAgent` / `IClient` interfaces with optional methods exposed as default interface
  methods that throw `MethodNotFound` until overridden.
- `AgentSideConnection` and `ClientSideConnection` high-level wrappers.
- `TerminalHandle` ergonomic `IAsyncDisposable` wrapper for terminal lifecycle.
- Extension escape hatch via `ExtMethodAsync` / `ExtNotificationAsync`.
- `_meta` field preserved on every type.
- Example `EchoAgent` (stdio) and `SampleClient` (subprocess walker) projects.
- 43 xUnit tests: serialization round-trips, ndjson framing edge cases, JSON-RPC
  correlation under concurrency, error-code mapping, and an Agent ↔ Client
  integration test.

### Out of scope
- The unstable surface marked `unstable_*` in the TypeScript SDK
  (NES, elicitation, providers, document sync, `session/set_model`).
  Reachable today via `ExtMethodAsync` / `ExtNotificationAsync`.
