# Changelog

All notable changes to this project will be documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Brings the library in line with the stable ACP v1 schema (upstream schema 1.9.x).

### Fixed (wire format — breaking)
- `current_mode_update` now writes/reads `currentModeId` (was `modeId`). The C# property is
  renamed `CurrentModeUpdate.CurrentModeId`.
- `session/set_config_option` request uses `configId` + `value` (+ `type: "boolean"` for boolean
  options) instead of `optionId` / `valueId`; the response field is `configOptions` (was
  `options`) and is typed as `IReadOnlyList<ConfigOption>`. Use
  `SetSessionConfigOptionRequest.Select(...)` / `.Boolean(...)`.
- `ConfigOption` understands grouped select options (`group` + nested `options`) and
  `description`; `ConfigOption.AllValues` flattens groups.
- `session/set_model` was routable from the client but not dispatched on the agent side.
- Unknown `sessionUpdate` kinds no longer throw (which dropped the whole notification); they
  surface as `UnknownSessionUpdate` with the raw JSON and round-trip unchanged.
- Build: dropped the explicit `Microsoft.SourceLink.GitHub` 8.0.0 reference (built into the SDK);
  its vulnerable `Microsoft.Build.Tasks.Git` dependency failed restore under warnings-as-errors.

### Added
- `session/delete` (`IAgent.DeleteSessionAsync`, `sessionCapabilities.delete`).
- `logout` (`IAgent.LogoutAsync`, `agentCapabilities.auth.logout`).
- `$/cancel_request` in `Connection`: cancelling an outgoing request's token notifies the peer;
  an incoming `$/cancel_request` cancels the handler's token and the request is answered with
  `-32800` (`RequestErrorException.RequestCancelled`).
- Session updates `usage_update` (`UsageUpdate`, `Cost`) and `config_option_update`
  (`ConfigOptionUpdate`).
- `messageId` on message/thought chunks; `name` on tool calls and tool call updates.
- Client capabilities `auth.terminal`, `session.configOptions.boolean`, `elicitation` (raw JSON);
  terminal auth method fields `type` / `args` / `env` on `AuthMethod`.
- `SessionInfo.additionalDirectories`, `ResumeSessionResponse.configOptions`.

### Deprecated
- `EndTurnUpdate` (`end_turn`) and `DiffUpdate` (`diff`): not part of ACP. Still decoded for
  compatibility, marked `[Obsolete]`.

### Not yet implemented
- `elicitation/create` / `elicitation/complete` (stable since upstream 1.7.0).

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
