using Acp;
using Acp.Schema;
using Acp.Streaming;

// EchoAgent: a minimal ACP agent that streams the user's prompt text back as agent message
// chunks. It demonstrates the typical agent lifecycle:
//   - bind stdio to the protocol via NdJsonStream + AgentSideConnection
//   - implement IAgent
//   - emit session/update notifications during a prompt turn
//   - respond to session/prompt with a stop reason
//
// All log output goes to stderr — stdout is reserved for the protocol.

var stream = NdJsonStream.FromStdio();
await using var connection = new AgentSideConnection(c => new EchoAgent(c), stream);
await connection.Closed;

internal sealed class EchoAgent(AgentSideConnection client) : IAgent
{
    private readonly AgentSideConnection _client = client;
    private int _nextSessionId;

    public Task<InitializeResponse> InitializeAsync(InitializeRequest request, CancellationToken cancellationToken)
    {
        Log($"initialize: clientProtocolVersion={request.ProtocolVersion}, " +
            $"client={request.ClientInfo?.Name ?? "?"}");
        return Task.FromResult(new InitializeResponse
        {
            ProtocolVersion = Protocol.Version,
            AgentInfo = new Implementation
            {
                Name = "echo-agent",
                Title = "Acp .NET EchoAgent",
                Version = "0.1.0",
            },
            AgentCapabilities = new AgentCapabilities
            {
                LoadSession = false,
                PromptCapabilities = new PromptCapabilities { Image = false, Audio = false, EmbeddedContext = false },
            },
        });
    }

    public Task<AuthenticateResponse?> AuthenticateAsync(AuthenticateRequest request, CancellationToken cancellationToken)
    {
        Log($"authenticate: methodId={request.MethodId}");
        return Task.FromResult<AuthenticateResponse?>(new AuthenticateResponse());
    }

    public Task<NewSessionResponse> NewSessionAsync(NewSessionRequest request, CancellationToken cancellationToken)
    {
        var id = new SessionId($"sess-{Interlocked.Increment(ref _nextSessionId)}");
        Log($"new session: cwd={request.Cwd}, mcpServers={request.McpServers.Count}, id={id.Value}");
        return Task.FromResult(new NewSessionResponse { SessionId = id });
    }

    public async Task<PromptResponse> PromptAsync(PromptRequest request, CancellationToken cancellationToken)
    {
        // Concatenate text content blocks from the prompt.
        string text = string.Concat(request.Prompt.OfType<TextContent>().Select(t => t.Text));
        Log($"prompt: session={(string)request.SessionId}, text=\"{Truncate(text, 60)}\"");

        // Stream the echo back as a sequence of small chunks.
        const int chunkSize = 16;
        for (int i = 0; i < text.Length; i += chunkSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string slice = text.Substring(i, Math.Min(chunkSize, text.Length - i));
            await _client.SessionUpdateAsync(new SessionNotification
            {
                SessionId = request.SessionId,
                Update = new AgentMessageChunk
                {
                    Content = new TextContent { Text = slice },
                },
            }, cancellationToken);
        }

        return new PromptResponse { StopReason = StopReason.EndTurn };
    }

    public Task CancelAsync(CancelNotification notification, CancellationToken cancellationToken)
    {
        Log($"cancel: session={(string)notification.SessionId}");
        return Task.CompletedTask;
    }

    private static void Log(string message) => Console.Error.WriteLine($"[echo-agent] {message}");

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
