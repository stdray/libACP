using System.Diagnostics;
using Acp;
using Acp.Schema;
using Acp.Streaming;

// SampleClient: launches the EchoAgent as a subprocess via stdio and walks the canonical
// happy path of the Agent Client Protocol:
//   1. initialize
//   2. session/new
//   3. session/prompt with a text content block
//   4. consume the streamed agent_message_chunk updates
//
// Usage:
//   dotnet run --project examples/SampleClient -- "your prompt text here"

string promptText = args.Length > 0 ? string.Join(' ', args) : "Hello, agent!";

string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string echoAgentDir = Path.Combine(repoRoot, "examples", "EchoAgent");
if (!Directory.Exists(echoAgentDir))
{
    Console.Error.WriteLine($"could not locate EchoAgent project at {echoAgentDir}");
    return 1;
}

var psi = new ProcessStartInfo
{
    FileName = "dotnet",
    Arguments = "run --project \"" + echoAgentDir + "\" --no-build --verbosity quiet",
    UseShellExecute = false,
    RedirectStandardInput = true,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    CreateNoWindow = true,
};

using var proc = Process.Start(psi)!;
proc.ErrorDataReceived += (_, e) =>
{
    if (e.Data is not null) Console.Error.WriteLine(e.Data);
};
proc.BeginErrorReadLine();

var stream = new NdJsonStream(proc.StandardOutput.BaseStream, proc.StandardInput.BaseStream, leaveOpen: false);

SampleClient? clientImpl = null;
await using var connection = new ClientSideConnection(c =>
{
    clientImpl = new SampleClient();
    return clientImpl;
}, stream);

InitializeResponse init = await connection.InitializeAsync(new InitializeRequest
{
    ProtocolVersion = Protocol.Version,
    ClientInfo = new Implementation
    {
        Name = "sample-client",
        Title = "Acp .NET Sample Client",
        Version = "0.1.0",
    },
    ClientCapabilities = new ClientCapabilities
    {
        Fs = new FileSystemCapabilities { ReadTextFile = false, WriteTextFile = false },
        Terminal = false,
    },
}, CancellationToken.None);

Console.WriteLine($"connected to {init.AgentInfo?.Name} (protocol={init.ProtocolVersion})");

NewSessionResponse session = await connection.NewSessionAsync(new NewSessionRequest
{
    Cwd = Environment.CurrentDirectory,
    McpServers = Array.Empty<McpServer>(),
}, CancellationToken.None);

Console.WriteLine($"session: {(string)session.SessionId}");
Console.WriteLine($"sending prompt: \"{promptText}\"");

PromptResponse response = await connection.PromptAsync(new PromptRequest
{
    SessionId = session.SessionId,
    Prompt = new ContentBlock[] { new TextContent { Text = promptText } },
}, CancellationToken.None);

Console.WriteLine();
Console.WriteLine($"-- end of turn (stopReason={response.StopReason}) --");
Console.WriteLine($"received {clientImpl?.ChunkCount ?? 0} chunk(s).");

try { proc.StandardInput.Close(); } catch { /* ignore */ }
if (!proc.WaitForExit(2000))
{
    try { proc.Kill(true); } catch { /* ignore */ }
}

return 0;

internal sealed class SampleClient : IClient
{
    public int ChunkCount { get; private set; }

    public Task SessionUpdateAsync(SessionNotification notification, CancellationToken cancellationToken)
    {
        switch (notification.Update)
        {
            case AgentMessageChunk amc when amc.Content is TextContent t:
                Console.Write(t.Text);
                ChunkCount++;
                break;
            case AgentThoughtChunk atc when atc.Content is TextContent t:
                Console.Error.WriteLine($"[thought] {t.Text}");
                break;
            case PlanUpdate pu:
                Console.Error.WriteLine($"[plan] {pu.Entries.Count} entries");
                break;
        }
        return Task.CompletedTask;
    }

    public Task<RequestPermissionResponse> RequestPermissionAsync(RequestPermissionRequest request, CancellationToken cancellationToken)
    {
        // Auto-cancel — this sample client doesn't prompt the user.
        return Task.FromResult(new RequestPermissionResponse { Outcome = new CancelledPermissionOutcome() });
    }
}
