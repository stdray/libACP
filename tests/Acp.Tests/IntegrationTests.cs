using System.Collections.Concurrent;
using Acp;
using Acp.Schema;

namespace Acp.Tests;

/// <summary>
/// Wires <see cref="AgentSideConnection"/> and <see cref="ClientSideConnection"/> through paired
/// in-memory ndjson streams and walks the canonical happy path of the protocol.
/// </summary>
public class IntegrationTests
{
    private sealed class TestAgent(AgentSideConnection client) : IAgent
    {
        private readonly AgentSideConnection _client = client;
        public Task<InitializeResponse> InitializeAsync(InitializeRequest req, CancellationToken ct) =>
            Task.FromResult(new InitializeResponse
            {
                ProtocolVersion = Protocol.Version,
                AgentInfo = new Implementation { Name = "test-agent" },
                AgentCapabilities = new AgentCapabilities { LoadSession = false },
            });

        public Task<AuthenticateResponse?> AuthenticateAsync(AuthenticateRequest r, CancellationToken ct) =>
            Task.FromResult<AuthenticateResponse?>(new AuthenticateResponse());

        public Task<NewSessionResponse> NewSessionAsync(NewSessionRequest r, CancellationToken ct) =>
            Task.FromResult(new NewSessionResponse { SessionId = new SessionId("sess-1") });

        public async Task<PromptResponse> PromptAsync(PromptRequest r, CancellationToken ct)
        {
            string text = string.Concat(r.Prompt.OfType<TextContent>().Select(t => t.Text));
            await _client.SessionUpdateAsync(new SessionNotification
            {
                SessionId = r.SessionId,
                Update = new AgentMessageChunk { Content = new TextContent { Text = "echo: " + text } },
            }, ct);
            return new PromptResponse { StopReason = StopReason.EndTurn };
        }

        public Task CancelAsync(CancelNotification n, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class TestClient : IClient
    {
        public TestClient() { }
        public TestClient(ClientSideConnection _) { }
        public ConcurrentQueue<SessionNotification> Updates { get; } = new();

        public Task SessionUpdateAsync(SessionNotification n, CancellationToken ct)
        {
            Updates.Enqueue(n);
            return Task.CompletedTask;
        }

        public Task<RequestPermissionResponse> RequestPermissionAsync(RequestPermissionRequest r, CancellationToken ct) =>
            Task.FromResult(new RequestPermissionResponse { Outcome = new CancelledPermissionOutcome() });
    }

    [Fact]
    public async Task EndToEnd_InitializeNewSessionPrompt_StreamsUpdate()
    {
        var (agentSide, clientSide) = PairedNdJsonStreams.Create();

        await using var agentConn = new AgentSideConnection(c => new TestAgent(c), agentSide);

        TestClient? clientInstance = null;
        await using var clientConn = new ClientSideConnection(c =>
        {
            clientInstance = new TestClient(c);
            return clientInstance;
        }, clientSide);

        InitializeResponse init = await clientConn.InitializeAsync(new InitializeRequest
        {
            ProtocolVersion = Protocol.Version,
            ClientInfo = new Implementation { Name = "test-client" },
            ClientCapabilities = new ClientCapabilities(),
        }, CancellationToken.None);
        Assert.Equal(Protocol.Version, init.ProtocolVersion);
        Assert.Equal("test-agent", init.AgentInfo!.Name);

        NewSessionResponse session = await clientConn.NewSessionAsync(new NewSessionRequest
        {
            Cwd = "/work",
            McpServers = Array.Empty<McpServer>(),
        }, CancellationToken.None);
        Assert.Equal("sess-1", (string)session.SessionId);

        PromptResponse prompt = await clientConn.PromptAsync(new PromptRequest
        {
            SessionId = session.SessionId,
            Prompt = new ContentBlock[] { new TextContent { Text = "hi" } },
        }, CancellationToken.None);
        Assert.Equal(StopReason.EndTurn, prompt.StopReason);

        // The notification is sent before the prompt response, but await a brief moment
        // to ensure the client-side handler has finished enqueuing.
        for (int i = 0; i < 20 && (clientInstance?.Updates.Count ?? 0) == 0; i++)
        {
            await Task.Delay(25);
        }
        Assert.NotNull(clientInstance);
        Assert.True(clientInstance!.Updates.TryDequeue(out var update));
        var chunk = Assert.IsType<AgentMessageChunk>(update!.Update);
        Assert.Equal("echo: hi", Assert.IsType<TextContent>(chunk.Content).Text);
    }

    [Fact]
    public async Task UnimplementedAgentMethod_ReturnsMethodNotFound()
    {
        var (agentSide, clientSide) = PairedNdJsonStreams.Create();
        await using var agentConn = new AgentSideConnection(c => new TestAgent(c), agentSide);
        await using var clientConn = new ClientSideConnection(c => new TestClient(c), clientSide);

        var ex = await Assert.ThrowsAsync<Acp.JsonRpc.RequestErrorException>(() =>
            clientConn.ListSessionsAsync(new ListSessionsRequest(), CancellationToken.None));
        Assert.Equal(-32601, ex.Code);
    }
}
