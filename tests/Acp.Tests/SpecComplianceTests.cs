using System.Text.Json;
using Acp;
using Acp.JsonRpc;
using Acp.Schema;

namespace Acp.Tests;

/// <summary>
/// Wire-shape checks against the stable ACP v1 schema, plus the methods and updates it added
/// after this library's first release.
/// </summary>
public class SpecComplianceTests
{
    private static JsonElement ToJson<T>(T value) => JsonSerializer.SerializeToElement(value, AcpJson.Options);

    private static T FromJson<T>(string json) => JsonSerializer.Deserialize<T>(json, AcpJson.Options)!;

    [Fact]
    public void CurrentModeUpdate_UsesCurrentModeIdOnTheWire()
    {
        JsonElement el = ToJson<SessionUpdate>(new CurrentModeUpdate { CurrentModeId = new SessionModeId("code") });
        Assert.Equal("current_mode_update", el.GetProperty("sessionUpdate").GetString());
        Assert.Equal("code", el.GetProperty("currentModeId").GetString());
        Assert.False(el.TryGetProperty("modeId", out _));
    }

    [Fact]
    public void UsageUpdate_RoundTrips()
    {
        var update = FromJson<SessionUpdate>(
            """{"sessionUpdate":"usage_update","used":1200,"size":200000,"cost":{"amount":0.42,"currency":"USD"}}""");
        var usage = Assert.IsType<UsageUpdate>(update);
        Assert.Equal(1200UL, usage.Used);
        Assert.Equal(200000UL, usage.Size);
        Assert.Equal("USD", usage.Cost!.Currency);

        JsonElement el = ToJson<SessionUpdate>(usage);
        Assert.Equal("usage_update", el.GetProperty("sessionUpdate").GetString());
        Assert.Equal(0.42, el.GetProperty("cost").GetProperty("amount").GetDouble());
    }

    [Fact]
    public void ConfigOptionUpdate_ReadsSelectAndBooleanOptions()
    {
        var update = FromJson<SessionUpdate>("""
            {"sessionUpdate":"config_option_update","configOptions":[
              {"id":"model","name":"Model","category":"model","type":"select","currentValue":"fast",
               "options":[
                 {"group":"anthropic","name":"Anthropic","options":[{"value":"fast","name":"Fast"},{"value":"smart","name":"Smart"}]},
                 {"group":"local","name":"Local","options":[{"value":"llama","name":"Llama"}]}]},
              {"id":"yolo","name":"Auto-approve","type":"boolean","currentValue":false}
            ]}
            """);
        var cfg = Assert.IsType<ConfigOptionUpdate>(update);
        Assert.Equal(2, cfg.ConfigOptions.Count);

        ConfigOption model = cfg.ConfigOptions[0];
        Assert.Equal("fast", model.CurrentValue!.Value.GetString());
        Assert.Equal(["fast", "smart", "llama"], model.AllValues.Select(v => v.Value));

        ConfigOption yolo = cfg.ConfigOptions[1];
        Assert.Equal(JsonValueKind.False, yolo.CurrentValue!.Value.ValueKind);
    }

    [Fact]
    public void ConfigOption_FlatOptions_AllValuesReturnsThem()
    {
        var opt = FromJson<ConfigOption>(
            """{"id":"mode","name":"Mode","type":"select","currentValue":"ask","options":[{"value":"ask","name":"Ask"},{"value":"code","name":"Code"}]}""");
        Assert.Equal(["ask", "code"], opt.AllValues.Select(v => v.Value));
    }

    [Fact]
    public void SetSessionConfigOptionRequest_MatchesSchema()
    {
        JsonElement select = ToJson(SetSessionConfigOptionRequest.Select(new SessionId("s"), "model", "smart"));
        Assert.Equal("model", select.GetProperty("configId").GetString());
        Assert.Equal("smart", select.GetProperty("value").GetString());
        Assert.False(select.TryGetProperty("type", out _));

        JsonElement boolean = ToJson(SetSessionConfigOptionRequest.Boolean(new SessionId("s"), "yolo", true));
        Assert.Equal("boolean", boolean.GetProperty("type").GetString());
        Assert.True(boolean.GetProperty("value").GetBoolean());
    }

    [Fact]
    public void SetSessionConfigOptionResponse_ReadsConfigOptions()
    {
        var resp = FromJson<SetSessionConfigOptionResponse>(
            """{"configOptions":[{"id":"yolo","name":"Auto-approve","type":"boolean","currentValue":true}]}""");
        Assert.Equal("yolo", Assert.Single(resp.ConfigOptions).Id);
    }

    [Fact]
    public void UnknownSessionUpdate_IsPreservedInsteadOfThrowing()
    {
        const string json = """{"sessionUpdate":"future_thing","x":1}""";
        var unknown = Assert.IsType<UnknownSessionUpdate>(FromJson<SessionUpdate>(json));
        Assert.Equal("future_thing", unknown.Kind);
        Assert.Equal(1, unknown.Raw.GetProperty("x").GetInt32());

        JsonElement el = ToJson<SessionUpdate>(unknown);
        Assert.Equal("future_thing", el.GetProperty("sessionUpdate").GetString());
        Assert.Equal(1, el.GetProperty("x").GetInt32());
    }

    [Fact]
    public void ContentChunk_AndToolCall_CarryNewOptionalFields()
    {
        var chunk = Assert.IsType<AgentMessageChunk>(FromJson<SessionUpdate>(
            """{"sessionUpdate":"agent_message_chunk","messageId":"m1","content":{"type":"text","text":"hi"}}"""));
        Assert.Equal("m1", chunk.MessageId);

        var call = Assert.IsType<ToolCallStartUpdate>(FromJson<SessionUpdate>(
            """{"sessionUpdate":"tool_call","toolCallId":"t1","title":"Read","name":"read_file"}"""));
        Assert.Equal("read_file", call.Name);
    }

    [Fact]
    public void Capabilities_DeleteLogoutAndClientAuth_RoundTrip()
    {
        JsonElement agent = ToJson(new AgentCapabilities
        {
            SessionCapabilities = new SessionCapabilities { Delete = new SessionDeleteCapabilities() },
            Auth = new AgentAuthCapabilities { Logout = new LogoutCapabilities() },
        });
        Assert.Equal(JsonValueKind.Object, agent.GetProperty("sessionCapabilities").GetProperty("delete").ValueKind);
        Assert.Equal(JsonValueKind.Object, agent.GetProperty("auth").GetProperty("logout").ValueKind);

        var client = FromJson<ClientCapabilities>(
            """{"auth":{"terminal":true},"session":{"configOptions":{"boolean":{}}},"elicitation":{"form":{}}}""");
        Assert.True(client.Auth!.Terminal);
        Assert.NotNull(client.Session!.ConfigOptions!.Boolean);
        Assert.Equal(JsonValueKind.Object, client.Elicitation!.Value.ValueKind);
    }

    [Fact]
    public void AuthMethod_TerminalVariant_RoundTrips()
    {
        var m = FromJson<AuthMethod>(
            """{"type":"terminal","id":"login","name":"Log in","args":["--login"],"env":{"MODE":"tty"}}""");
        Assert.Equal("terminal", m.Type);
        Assert.Equal(["--login"], m.Args);
        Assert.Equal("tty", m.Env!["MODE"]);
    }

    // --- methods over a real connection pair ---

    private sealed class LifecycleAgent : IAgent
    {
        public List<string> Deleted { get; } = [];
        public int Logouts;

        public Task<InitializeResponse> InitializeAsync(InitializeRequest r, CancellationToken ct) =>
            Task.FromResult(new InitializeResponse { ProtocolVersion = Protocol.Version });
        public Task<AuthenticateResponse?> AuthenticateAsync(AuthenticateRequest r, CancellationToken ct) =>
            Task.FromResult<AuthenticateResponse?>(null);
        public Task<NewSessionResponse> NewSessionAsync(NewSessionRequest r, CancellationToken ct) =>
            Task.FromResult(new NewSessionResponse { SessionId = new SessionId("s") });
        public Task<PromptResponse> PromptAsync(PromptRequest r, CancellationToken ct) =>
            Task.FromResult(new PromptResponse { StopReason = StopReason.EndTurn });
        public Task CancelAsync(CancelNotification n, CancellationToken ct) => Task.CompletedTask;

        public Task<DeleteSessionResponse?> DeleteSessionAsync(DeleteSessionRequest r, CancellationToken ct)
        {
            Deleted.Add(r.SessionId.Value);
            return Task.FromResult<DeleteSessionResponse?>(null);
        }

        public Task<LogoutResponse?> LogoutAsync(LogoutRequest r, CancellationToken ct)
        {
            Interlocked.Increment(ref Logouts);
            return Task.FromResult<LogoutResponse?>(null);
        }

        public Task<SetSessionConfigOptionResponse> SetSessionConfigOptionAsync(SetSessionConfigOptionRequest r, CancellationToken ct) =>
            Task.FromResult(new SetSessionConfigOptionResponse
            {
                ConfigOptions = [new ConfigOption { Id = r.ConfigId, Name = r.ConfigId, Type = r.Type ?? "select", CurrentValue = r.Value }],
            });
    }

    private sealed class NullClient : IClient
    {
        public Task SessionUpdateAsync(SessionNotification n, CancellationToken ct) => Task.CompletedTask;
        public Task<RequestPermissionResponse> RequestPermissionAsync(RequestPermissionRequest r, CancellationToken ct) =>
            Task.FromResult(new RequestPermissionResponse { Outcome = new CancelledPermissionOutcome() });
    }

    [Fact]
    public async Task DeleteLogoutAndSetConfigOption_RoundTripOverConnection()
    {
        var (agentSide, clientSide) = PairedNdJsonStreams.Create();
        var agent = new LifecycleAgent();
        await using var agentConn = new AgentSideConnection(_ => agent, agentSide);
        await using var clientConn = new ClientSideConnection(_ => new NullClient(), clientSide);

        DeleteSessionResponse? deleted = await clientConn.DeleteSessionAsync(new DeleteSessionRequest { SessionId = new SessionId("old") }, CancellationToken.None);
        Assert.NotNull(deleted);
        Assert.Equal(["old"], agent.Deleted);

        Assert.NotNull(await clientConn.LogoutAsync(new LogoutRequest(), CancellationToken.None));
        Assert.Equal(1, agent.Logouts);

        SetSessionConfigOptionResponse cfg = await clientConn.SetSessionConfigOptionAsync(
            SetSessionConfigOptionRequest.Boolean(new SessionId("s"), "yolo", true), CancellationToken.None);
        ConfigOption opt = Assert.Single(cfg.ConfigOptions);
        Assert.Equal("boolean", opt.Type);
        Assert.True(opt.CurrentValue!.Value.GetBoolean());
    }

    [Fact]
    public async Task UnimplementedDelete_IsMethodNotFound()
    {
        var (agentSide, clientSide) = PairedNdJsonStreams.Create();
        await using var agentConn = new AgentSideConnection(_ => new MinimalAgent(), agentSide);
        await using var clientConn = new ClientSideConnection(_ => new NullClient(), clientSide);

        var ex = await Assert.ThrowsAsync<RequestErrorException>(() =>
            clientConn.DeleteSessionAsync(new DeleteSessionRequest { SessionId = new SessionId("x") }, CancellationToken.None));
        Assert.Equal(-32601, ex.Code);
    }

    private sealed class MinimalAgent : IAgent
    {
        public Task<InitializeResponse> InitializeAsync(InitializeRequest r, CancellationToken ct) =>
            Task.FromResult(new InitializeResponse { ProtocolVersion = Protocol.Version });
        public Task<AuthenticateResponse?> AuthenticateAsync(AuthenticateRequest r, CancellationToken ct) =>
            Task.FromResult<AuthenticateResponse?>(null);
        public Task<NewSessionResponse> NewSessionAsync(NewSessionRequest r, CancellationToken ct) =>
            Task.FromResult(new NewSessionResponse { SessionId = new SessionId("s") });
        public Task<PromptResponse> PromptAsync(PromptRequest r, CancellationToken ct) =>
            Task.FromResult(new PromptResponse { StopReason = StopReason.EndTurn });
        public Task CancelAsync(CancelNotification n, CancellationToken ct) => Task.CompletedTask;
    }

    // --- $/cancel_request ---

    [Fact]
    public async Task CancellingOutgoingRequest_SendsCancelRequest_AndCancelsPeerHandler()
    {
        var (a, b) = PairedNdJsonStreams.Create();
        var handlerCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handlerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var server = new Connection(a,
            requestHandler: async (_, _, ct) =>
            {
                handlerStarted.SetResult();
                try
                {
                    await Task.Delay(Timeout.Infinite, ct);
                }
                catch (OperationCanceledException)
                {
                    handlerCancelled.SetResult();
                    throw;
                }
                return null;
            },
            notificationHandler: (_, _, _) => Task.CompletedTask);
        server.Start();

        await using var client = new Connection(b,
            requestHandler: (_, _, _) => Task.FromResult<object?>(null),
            notificationHandler: (_, _, _) => Task.CompletedTask);
        client.Start();

        using var cts = new CancellationTokenSource();
        Task<JsonElement> call = client.SendRequestAsync<JsonElement>("slow", null, cts.Token);
        await handlerStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => call);
        await handlerCancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task IncomingCancelRequest_AnswersWithRequestCancelled()
    {
        var (a, b) = PairedNdJsonStreams.Create();
        var handlerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var server = new Connection(a,
            requestHandler: async (_, _, ct) =>
            {
                handlerStarted.SetResult();
                await Task.Delay(Timeout.Infinite, ct);
                return null;
            },
            notificationHandler: (_, _, _) => Task.CompletedTask);
        server.Start();

        await using var client = new Connection(b,
            requestHandler: (_, _, _) => Task.FromResult<object?>(null),
            notificationHandler: (_, _, _) => Task.CompletedTask);
        client.Start();

        // The client's first request gets id 1; cancel it by hand so the local side keeps waiting for the reply.
        Task<JsonElement> call = client.SendRequestAsync<JsonElement>("slow", null);
        await handlerStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await client.SendNotificationAsync(ProtocolMethods.CancelRequest, new { requestId = 1 });

        var ex = await Assert.ThrowsAsync<RequestErrorException>(() => call.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(-32800, ex.Code);
    }
}
