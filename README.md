# Acp — Agent Client Protocol for .NET

[![ci](https://github.com/sargeMonkey/libACP/actions/workflows/ci.yml/badge.svg)](https://github.com/sargeMonkey/libACP/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/LibAcp.svg)](https://www.nuget.org/packages/LibAcp)
[![NuGet downloads](https://img.shields.io/nuget/dt/LibAcp.svg)](https://www.nuget.org/packages/LibAcp)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%2010.0-512BD4)](https://dotnet.microsoft.com/)

An idiomatic C# / .NET implementation of the [Agent Client Protocol (ACP)](https://agentclientprotocol.com),
modelled after the official [TypeScript SDK](https://github.com/agentclientprotocol/typescript-sdk).

ACP is a JSON-RPC 2.0 protocol that standardises how code editors ("clients") talk to coding
agents ("agents"). This library lets you build either side in .NET 8+ (multi-targets `net8.0`
and `net10.0`).

> **Status:** unaffiliated community port. See [Related projects](#related-projects) below for
> the other community .NET implementations.

## Install

```pwsh
dotnet add package LibAcp
```

Or reference the source directly:

```pwsh
git clone https://github.com/sargeMonkey/libACP.git
cd libACP
dotnet build Acp.slnx
```

> The NuGet package id is **`LibAcp`** but the assembly name and root namespace remain
> `Acp`, so your `using Acp;` directives work unchanged.

## Features

- Full coverage of the **stable** ACP surface (protocol version `1`):
  - `initialize`, `authenticate`
  - `session/new`, `session/load`, `session/resume`, `session/list`, `session/close`, `session/delete`, `logout`
  - `session/prompt`, `session/cancel`, `session/update`
  - `session/set_mode`, `session/set_config_option`, `session/request_permission`
  - `fs/read_text_file`, `fs/write_text_file`
  - `terminal/create`, `terminal/output`, `terminal/release`, `terminal/wait_for_exit`, `terminal/kill`
  - Session updates incl. `usage_update` and `config_option_update`; unknown kinds are preserved as `UnknownSessionUpdate`
  - `$/cancel_request` in both directions (`-32800` Request cancelled)
  - `elicitation/create` (form and URL modes), `elicitation/complete`, `clientCapabilities.elicitation`
  - Extension escape hatch via `extMethod` / `extNotification`
  - The `_meta` field is preserved on every type
- `System.Text.Json` end-to-end (no Newtonsoft dependency)
- Discriminated-union JSON converters for `ContentBlock`, `SessionUpdate`,
  `RequestPermissionOutcome`, `ToolCallContent`, `EmbeddedResourceResource`, `McpServer`, `RequestId`
- Newline-delimited JSON transport over any `System.IO.Stream` pair (typically stdio)
- Concurrent requests are correlated by id; cancelling a request's token also sends `$/cancel_request` to the peer
- IL-only logging hooks via `Microsoft.Extensions.Logging.Abstractions`

## Layout

```
src/
  Acp/                       # The library (net8.0 / net10.0 multi-target)
tests/
  Acp.Tests/                 # xUnit tests (78 tests)
examples/
  EchoAgent/                 # Stdio agent that streams the prompt back
  SampleClient/              # Spawns the EchoAgent and walks the protocol
.github/workflows/           # CI: build + test on linux/windows/mac, pack
```

## Build

Requires the .NET 8 SDK (or .NET 10 SDK for the second target).

```pwsh
dotnet build Acp.slnx
dotnet test  tests/Acp.Tests/Acp.Tests.csproj
```

## Quick start — writing an agent

```csharp
using Acp;
using Acp.Schema;
using Acp.Streaming;

var stream = NdJsonStream.FromStdio();
await using var connection = new AgentSideConnection(c => new MyAgent(c), stream);
await connection.Closed;

internal sealed class MyAgent(AgentSideConnection client) : IAgent
{
    public Task<InitializeResponse> InitializeAsync(InitializeRequest req, CancellationToken ct)
        => Task.FromResult(new InitializeResponse
        {
            ProtocolVersion = Protocol.Version,
            AgentInfo = new Implementation { Name = "my-agent", Version = "0.1.0" },
            AgentCapabilities = new AgentCapabilities(),
        });

    public Task<NewSessionResponse> NewSessionAsync(NewSessionRequest req, CancellationToken ct)
        => Task.FromResult(new NewSessionResponse { SessionId = new SessionId(Guid.NewGuid().ToString("n")) });

    public async Task<PromptResponse> PromptAsync(PromptRequest req, CancellationToken ct)
    {
        await client.SessionUpdateAsync(new SessionNotification
        {
            SessionId = req.SessionId,
            Update    = new AgentMessageChunk { Content = new TextContent { Text = "hello!" } },
        }, ct);
        return new PromptResponse { StopReason = StopReason.EndTurn };
    }

    public Task<AuthenticateResponse?> AuthenticateAsync(AuthenticateRequest req, CancellationToken ct)
        => Task.FromResult<AuthenticateResponse?>(new AuthenticateResponse());

    public Task CancelAsync(CancelNotification n, CancellationToken ct) => Task.CompletedTask;
}
```

Optional methods (`session/load`, `session/list`, `terminal/*`, etc.) are provided as default
interface methods that throw `MethodNotFound` until you override them.

> ⚠ **Never write to `Console.Out`** in an agent — stdout is the protocol channel. Send all logs
> to `Console.Error` (stderr) instead.

## Quick start — writing a client

```csharp
using System.Diagnostics;
using Acp;
using Acp.Schema;
using Acp.Streaming;

var psi = new ProcessStartInfo("path/to/agent")
{
    UseShellExecute = false,
    RedirectStandardInput = true,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
};
using var proc = Process.Start(psi)!;
var stream = new NdJsonStream(proc.StandardOutput.BaseStream, proc.StandardInput.BaseStream);

await using var connection = new ClientSideConnection(c => new MyClient(), stream);

await connection.InitializeAsync(new InitializeRequest
{
    ProtocolVersion = Protocol.Version,
    ClientInfo = new Implementation { Name = "my-client", Version = "0.1.0" },
    ClientCapabilities = new ClientCapabilities(),
}, CancellationToken.None);

var session = await connection.NewSessionAsync(new NewSessionRequest
{
    Cwd = Environment.CurrentDirectory,
    McpServers = Array.Empty<McpServer>(),
}, CancellationToken.None);

var resp = await connection.PromptAsync(new PromptRequest
{
    SessionId = session.SessionId,
    Prompt = new ContentBlock[] { new TextContent { Text = "Hello, agent!" } },
}, CancellationToken.None);

internal sealed class MyClient : IClient
{
    public Task SessionUpdateAsync(SessionNotification n, CancellationToken ct)
    {
        if (n.Update is AgentMessageChunk amc && amc.Content is TextContent t)
            Console.Write(t.Text);
        return Task.CompletedTask;
    }

    public Task<RequestPermissionResponse> RequestPermissionAsync(RequestPermissionRequest r, CancellationToken ct)
        => Task.FromResult(new RequestPermissionResponse { Outcome = new CancelledPermissionOutcome() });
}
```

## Running the bundled example

```pwsh
dotnet build Acp.slnx
dotnet run --project examples/SampleClient -- "Hello from the .NET ACP client!"
```

You should see the prompt echoed back, chunk by chunk, with the agent's stderr log lines and a
final `stopReason=EndTurn` summary.

## Architecture notes

- **Transport**: `IMessageStream` defines `ReadAsync` / `WriteAsync` over JSON-RPC envelopes. The
  default `NdJsonStream` handles newline framing (with `\r\n` tolerance), enforces a 16 MiB max
  line length, and parks parse errors as `RequestErrorException(-32700)` so the connection can
  reply to the peer rather than crashing.
- **Connection**: `Connection` runs a single background receive loop, correlates requests by id
  in a `ConcurrentDictionary`, normalises empty void responses to `{}`, and maps thrown
  `RequestErrorException`s onto the JSON-RPC error envelope. Other handler exceptions become
  `-32603 internal error` with the message in `error.data.details`.
- **Schema (DTOs)**: immutable C# `record`s with `[JsonPropertyName]`, `required` for required
  fields, nullable for optional. Discriminated unions use abstract base records + a custom
  `JsonConverter` that switches on the discriminator field (`type`, `sessionUpdate`, `outcome`,
  ...) and dispatches to the right concrete record.
- **Routing**: `AgentSideConnection` and `ClientSideConnection` route inbound requests by
  `switch`-on-method-name (matches the TS SDK), not reflection.

## Tests

43 xUnit tests covering serialization round-trips, NdJson framing edge cases, JSON-RPC
correlation under concurrency, error-code mapping, and a full Agent ↔ Client integration.

```pwsh
dotnet test tests/Acp.Tests/Acp.Tests.csproj
```

## Out of scope

The unstable surface that the TS SDK marks `unstable_*` is intentionally **not** modelled in this
release: NES, providers, document sync (`document/did*`), and `session/set_model`.
You can still reach them via the `extMethod` / `extNotification` escape hatch.

## Related projects

Other community .NET implementations of ACP (as of mid-2026):

- **[nuskey8/acp-csharp](https://github.com/nuskey8/acp-csharp)** — published as
  [`AgentClientProtocol`](https://www.nuget.org/packages/AgentClientProtocol) on NuGet.
  Similar shape (`IAcpClient`/`IAcpAgent`, `ClientSideConnection`/`AgentSideConnection`).
  Used in production by `nuskey8/UnityAgentClient`.
- **`AgentClientProtocol.*`** family (e.g. `AgentClientProtocol.Agent`,
  `AgentClientProtocol.Client`) — date-versioned packages auto-generated from the official
  ACP JSON Schema.

This package (`LibAcp`) differs in being:

- Hand-written rather than generated, with idiomatic C# `record`s and discriminated-union
  converters chosen per the schema.
- Multi-targeted at `net8.0` (LTS) and `net10.0` (current LTS).
- Conservative about scope: only the **stable** protocol surface is modelled as first-class
  DTOs; unstable surface (`unstable_*`) is reachable via `ExtMethodAsync` /
  `ExtNotificationAsync`.

Pick whichever fits your project best — the protocol is the same.

## License

[MIT](LICENSE). This is a community implementation. The protocol itself is governed by the
[ACP project](https://agentclientprotocol.com).

## Contributing

Issues and pull requests welcome. By contributing you agree your work will be released under
the MIT license.

## Releasing

See [PUBLISHING.md](PUBLISHING.md) for how to cut a release and push to nuget.org. The
[`release.yml`](.github/workflows/release.yml) workflow builds, tests, packs and pushes on any
`vX.Y.Z` git tag.
