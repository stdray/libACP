using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using Acp;
using Acp.JsonRpc;
using Acp.Schema;

namespace Acp.Tests;

/// <summary>Wire-shape and end-to-end tests for <c>elicitation/create</c> and <c>elicitation/complete</c>.</summary>
public class ElicitationTests
{
    private static JsonElement ToJson<T>(T value) => JsonSerializer.SerializeToElement(value, AcpJson.Options);

    private static T FromJson<T>(string json) => JsonSerializer.Deserialize<T>(json, AcpJson.Options)!;

    private static void AssertJsonEqual(string expected, JsonElement actual) =>
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(expected), JsonNode.Parse(actual.GetRawText())), actual.GetRawText());

    // --- capabilities ---

    [Fact]
    public void Capabilities_FormAndUrl_RoundTrip()
    {
        var caps = FromJson<ClientCapabilities>("""{"elicitation":{"form":{},"url":{}}}""");
        Assert.NotNull(caps.Elicitation!.Form);
        Assert.NotNull(caps.Elicitation.Url);
        AssertJsonEqual("""{"elicitation":{"form":{},"url":{}}}""", ToJson(caps));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"form":null,"url":null}""")]
    public void Capabilities_EmptyOrNull_AdvertiseNoModes(string json)
    {
        var caps = FromJson<ElicitationCapabilities>(json);
        Assert.Null(caps.Form);
        Assert.Null(caps.Url);
    }

    // --- elicitation/create request ---

    private const string FormRequestJson = """
        {
          "sessionId": "sess_abc123",
          "mode": "form",
          "message": "How should I approach this refactoring?",
          "requestedSchema": {
            "type": "object",
            "properties": {
              "strategy": {
                "type": "string",
                "enum": ["conservative", "balanced", "aggressive"]
              }
            },
            "required": ["strategy"]
          }
        }
        """;

    private const string UrlRequestJson = """
        {
          "requestId": 12,
          "mode": "url",
          "elicitationId": "github-oauth-001",
          "url": "https://agent.example.com/connect?elicitationId=github-oauth-001",
          "message": "Please authorize access to your repositories."
        }
        """;

    [Fact]
    public void FormRequest_DocExample_Parses()
    {
        var form = Assert.IsType<FormElicitationRequest>(FromJson<CreateElicitationRequest>(FormRequestJson));
        Assert.Equal("form", form.Mode);
        Assert.Equal("How should I approach this refactoring?", form.Message);
        var scope = Assert.IsType<ElicitationSessionScope>(form.Scope);
        Assert.Equal("sess_abc123", scope.SessionId.Value);
        Assert.Null(scope.ToolCallId);
        Assert.Equal("object", form.RequestedSchema.Type);
        Assert.Equal(["strategy"], form.RequestedSchema.Required);
        var strategy = Assert.IsType<StringPropertySchema>(form.RequestedSchema.Properties["strategy"]);
        Assert.Equal(["conservative", "balanced", "aggressive"], strategy.Enum);
    }

    [Fact]
    public void FormRequest_DocExample_RoundTrips()
    {
        var req = FromJson<CreateElicitationRequest>(FormRequestJson);
        AssertJsonEqual(FormRequestJson, ToJson(req));
    }

    [Fact]
    public void FormRequest_SerializedByRuntimeType_KeepsDiscriminatorAndScope()
    {
        var req = new FormElicitationRequest
        {
            Message = "Pick",
            Scope = ElicitationScope.ForSession(new SessionId("s1"), new ToolCallId("tc1")),
            RequestedSchema = new ElicitationSchema(),
        };
        // The JSON-RPC layer serializes params by runtime type.
        JsonElement el = JsonSerializer.SerializeToElement(req, req.GetType(), AcpJson.Options);
        AssertJsonEqual(
            """{"mode":"form","message":"Pick","sessionId":"s1","toolCallId":"tc1","requestedSchema":{"type":"object","properties":{}}}""",
            el);
    }

    [Fact]
    public void UrlRequest_DocExample_ParsesAndRoundTrips()
    {
        var url = Assert.IsType<UrlElicitationRequest>(FromJson<CreateElicitationRequest>(UrlRequestJson));
        Assert.Equal("url", url.Mode);
        Assert.Equal("github-oauth-001", url.ElicitationId.Value);
        Assert.Equal("https://agent.example.com/connect?elicitationId=github-oauth-001", url.Url);
        var scope = Assert.IsType<ElicitationRequestScope>(url.Scope);
        Assert.Equal(RequestId.FromNumber(12), scope.RequestId);

        AssertJsonEqual(UrlRequestJson, ToJson<CreateElicitationRequest>(url));
    }

    [Fact]
    public void Request_UnknownMode_IsPreserved()
    {
        const string json = """{"mode":"telepathy","message":"hi","requestId":"r1","extra":{"x":1}}""";
        var unknown = Assert.IsType<UnknownElicitationRequest>(FromJson<CreateElicitationRequest>(json));
        Assert.Equal("telepathy", unknown.Mode);
        Assert.Equal("hi", unknown.Message);
        Assert.Equal(RequestId.FromString("r1"), Assert.IsType<ElicitationRequestScope>(unknown.Scope).RequestId);
        AssertJsonEqual(json, ToJson<CreateElicitationRequest>(unknown));
    }

    [Fact]
    public void Request_WithoutScope_IsRejected()
    {
        Assert.Throws<JsonException>(() => FromJson<CreateElicitationRequest>("""{"mode":"url","message":"m","elicitationId":"e","url":"https://x"}"""));
    }

    [Fact]
    public void RequestedSchema_AllPropertyKinds_RoundTrip()
    {
        const string json = """
            {
              "type": "object",
              "title": "Settings",
              "properties": {
                "email": {"type":"string","title":"Email","format":"email","minLength":3,"maxLength":64,"default":"a@b.c"},
                "size": {"type":"string","oneOf":[{"const":"s","title":"Small"},{"const":"l","title":"Large","description":"Big"}]},
                "ratio": {"type":"number","minimum":0.25,"maximum":1.5,"default":0.5},
                "count": {"type":"integer","minimum":1,"maximum":10,"default":3},
                "ok": {"type":"boolean","description":"Confirm","default":true},
                "tags": {"type":"array","minItems":1,"maxItems":2,"items":{"type":"string","enum":["a","b"]},"default":["a"]},
                "langs": {"type":"array","items":{"anyOf":[{"const":"cs","title":"C#"}]}},
                "future": {"type":"color","palette":"rgb"}
              },
              "required": ["email"]
            }
            """;
        var schema = FromJson<ElicitationSchema>(json);
        var props = schema.Properties;
        Assert.Equal(ElicitationStringFormat.Email, Assert.IsType<StringPropertySchema>(props["email"]).Format);
        Assert.Equal("Large", Assert.IsType<StringPropertySchema>(props["size"]).OneOf![1].Title);
        Assert.Equal(1.5, Assert.IsType<NumberPropertySchema>(props["ratio"]).Maximum);
        Assert.Equal(3L, Assert.IsType<IntegerPropertySchema>(props["count"]).Default);
        Assert.True(Assert.IsType<BooleanPropertySchema>(props["ok"]).Default);
        var tags = Assert.IsType<MultiSelectPropertySchema>(props["tags"]);
        Assert.Equal(["a", "b"], Assert.IsType<StringMultiSelectItems>(tags.Items).Enum);
        var langs = Assert.IsType<MultiSelectPropertySchema>(props["langs"]);
        Assert.Equal("cs", Assert.IsType<TitledMultiSelectItems>(langs.Items).AnyOf[0].Const);
        Assert.Equal("color", Assert.IsType<UnknownPropertySchema>(props["future"]).SchemaType);

        AssertJsonEqual(json, ToJson(schema));
    }

    // --- elicitation/create response ---

    [Fact]
    public void AcceptResponse_WithContent_RoundTrips()
    {
        const string json = """{"action":"accept","content":{"strategy":"balanced","n":3,"r":0.5,"ok":true,"tags":["a","b"]}}""";
        var accept = Assert.IsType<AcceptElicitationResponse>(FromJson<CreateElicitationResponse>(json));
        Assert.Equal("balanced", accept.Content!["strategy"].AsString());
        Assert.Equal(ElicitationContentValueKind.Integer, accept.Content["n"].Kind);
        Assert.Equal(3L, accept.Content["n"].AsInteger());
        Assert.Equal(0.5, accept.Content["r"].AsNumber());
        Assert.True(accept.Content["ok"].AsBoolean());
        Assert.Equal(["a", "b"], accept.Content["tags"].AsStringArray());
        AssertJsonEqual(json, ToJson<CreateElicitationResponse>(accept));
    }

    [Fact]
    public void AcceptResponse_WithoutContent_OmitsIt()
    {
        var accept = Assert.IsType<AcceptElicitationResponse>(FromJson<CreateElicitationResponse>("""{"action":"accept","content":null}"""));
        Assert.Null(accept.Content);
        AssertJsonEqual("""{"action":"accept"}""", ToJson<CreateElicitationResponse>(accept));
    }

    [Theory]
    [InlineData("decline", typeof(DeclineElicitationResponse))]
    [InlineData("cancel", typeof(CancelElicitationResponse))]
    public void DeclineAndCancel_RoundTrip(string action, Type expected)
    {
        string json = $$"""{"action":"{{action}}"}""";
        CreateElicitationResponse resp = FromJson<CreateElicitationResponse>(json);
        Assert.IsType(expected, resp);
        Assert.Equal(action, resp.Action);
        AssertJsonEqual(json, JsonSerializer.SerializeToElement(resp, resp.GetType(), AcpJson.Options));
    }

    [Fact]
    public void Response_UnknownAction_IsPreserved()
    {
        const string json = """{"action":"defer","until":"later"}""";
        var unknown = Assert.IsType<UnknownElicitationResponse>(FromJson<CreateElicitationResponse>(json));
        Assert.Equal("defer", unknown.Action);
        AssertJsonEqual(json, ToJson<CreateElicitationResponse>(unknown));
    }

    // --- elicitation/complete ---

    [Fact]
    public void CompleteNotification_RoundTrips()
    {
        const string json = """{"elicitationId":"github-oauth-001"}""";
        var n = FromJson<CompleteElicitationNotification>(json);
        Assert.Equal("github-oauth-001", n.ElicitationId.Value);
        AssertJsonEqual(json, ToJson(n));
    }

    // --- end to end ---

    private sealed class ElicitingAgent(AgentSideConnection client) : IAgent
    {
        public Task<InitializeResponse> InitializeAsync(InitializeRequest r, CancellationToken ct) =>
            Task.FromResult(new InitializeResponse { ProtocolVersion = Protocol.Version });
        public Task<AuthenticateResponse?> AuthenticateAsync(AuthenticateRequest r, CancellationToken ct) =>
            Task.FromResult<AuthenticateResponse?>(null);
        public Task<NewSessionResponse> NewSessionAsync(NewSessionRequest r, CancellationToken ct) =>
            Task.FromResult(new NewSessionResponse { SessionId = new SessionId("sess-1") });
        public Task CancelAsync(CancelNotification n, CancellationToken ct) => Task.CompletedTask;

        public async Task<PromptResponse> PromptAsync(PromptRequest r, CancellationToken ct)
        {
            CreateElicitationResponse form = await client.CreateElicitationAsync(new FormElicitationRequest
            {
                Message = "Strategy?",
                Scope = ElicitationScope.ForSession(r.SessionId),
                RequestedSchema = new ElicitationSchema
                {
                    Properties = new Dictionary<string, ElicitationPropertySchema>
                    {
                        ["strategy"] = new StringPropertySchema { Enum = ["conservative", "balanced"] },
                    },
                    Required = ["strategy"],
                },
            }, ct);
            string strategy = ((AcceptElicitationResponse)form).Content!["strategy"].AsString();

            CreateElicitationResponse url = await client.CreateElicitationAsync(new UrlElicitationRequest
            {
                Message = "Authorize",
                Scope = ElicitationScope.ForSession(r.SessionId),
                ElicitationId = new ElicitationId("oauth-1"),
                Url = "https://agent.example.com/connect",
            }, ct);
            await client.CompleteElicitationAsync(new CompleteElicitationNotification { ElicitationId = new ElicitationId("oauth-1") }, ct);

            await client.SessionUpdateAsync(new SessionNotification
            {
                SessionId = r.SessionId,
                Update = new AgentMessageChunk { Content = new TextContent { Text = $"{strategy}/{url.Action}" } },
            }, ct);
            return new PromptResponse { StopReason = StopReason.EndTurn };
        }
    }

    private sealed class ElicitationClient : IClient
    {
        public ConcurrentQueue<CreateElicitationRequest> Requests { get; } = new();
        public TaskCompletionSource<CompleteElicitationNotification> Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<string> Text { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task SessionUpdateAsync(SessionNotification n, CancellationToken ct)
        {
            if (n.Update is AgentMessageChunk { Content: TextContent t }) Text.TrySetResult(t.Text);
            return Task.CompletedTask;
        }

        public Task<RequestPermissionResponse> RequestPermissionAsync(RequestPermissionRequest r, CancellationToken ct) =>
            Task.FromResult(new RequestPermissionResponse { Outcome = new CancelledPermissionOutcome() });

        public Task<CreateElicitationResponse> CreateElicitationAsync(CreateElicitationRequest request, CancellationToken ct)
        {
            Requests.Enqueue(request);
            return Task.FromResult<CreateElicitationResponse>(request switch
            {
                FormElicitationRequest => new AcceptElicitationResponse
                {
                    Content = new Dictionary<string, ElicitationContentValue> { ["strategy"] = "balanced" },
                },
                UrlElicitationRequest => new DeclineElicitationResponse(),
                _ => new CancelElicitationResponse(),
            });
        }

        public Task CompleteElicitationAsync(CompleteElicitationNotification n, CancellationToken ct)
        {
            Completed.TrySetResult(n);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task EndToEnd_FormAndUrlElicitation_WithCompletion()
    {
        var (agentSide, clientSide) = PairedNdJsonStreams.Create();
        await using var agentConn = new AgentSideConnection(c => new ElicitingAgent(c), agentSide);
        var client = new ElicitationClient();
        await using var clientConn = new ClientSideConnection(_ => client, clientSide);

        await clientConn.InitializeAsync(new InitializeRequest
        {
            ProtocolVersion = Protocol.Version,
            ClientCapabilities = new ClientCapabilities
            {
                Elicitation = new ElicitationCapabilities { Form = new(), Url = new() },
            },
        }, CancellationToken.None);
        NewSessionResponse session = await clientConn.NewSessionAsync(new NewSessionRequest { Cwd = "/tmp", McpServers = [] }, CancellationToken.None);

        PromptResponse resp = await clientConn.PromptAsync(new PromptRequest
        {
            SessionId = session.SessionId,
            Prompt = [new TextContent { Text = "go" }],
        }, CancellationToken.None);

        Assert.Equal(StopReason.EndTurn, resp.StopReason);
        Assert.Equal("balanced/decline", await client.Text.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        CompleteElicitationNotification done = await client.Completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("oauth-1", done.ElicitationId.Value);

        CreateElicitationRequest[] reqs = client.Requests.ToArray();
        Assert.Equal(2, reqs.Length);
        var form = Assert.IsType<FormElicitationRequest>(reqs[0]);
        Assert.Equal("sess-1", Assert.IsType<ElicitationSessionScope>(form.Scope).SessionId.Value);
        Assert.IsType<StringPropertySchema>(form.RequestedSchema.Properties["strategy"]);
        Assert.Equal("https://agent.example.com/connect", Assert.IsType<UrlElicitationRequest>(reqs[1]).Url);
    }

    [Fact]
    public async Task CreateElicitation_DefaultClient_ReturnsMethodNotFound()
    {
        var (agentSide, clientSide) = PairedNdJsonStreams.Create();
        AgentSideConnection? agentRef = null;
        await using var agentConn = new AgentSideConnection(c => { agentRef = c; return new ElicitingAgent(c); }, agentSide);
        await using var clientConn = new ClientSideConnection(_ => new MinimalClient(), clientSide);

        var ex = await Assert.ThrowsAsync<RequestErrorException>(() => agentRef!.CreateElicitationAsync(new UrlElicitationRequest
        {
            Message = "m",
            Scope = ElicitationScope.ForRequest(RequestId.FromNumber(1)),
            ElicitationId = new ElicitationId("e"),
            Url = "https://x",
        }, CancellationToken.None));
        Assert.Equal(-32601, ex.Code);
    }

    private sealed class MinimalClient : IClient
    {
        public Task SessionUpdateAsync(SessionNotification n, CancellationToken ct) => Task.CompletedTask;
        public Task<RequestPermissionResponse> RequestPermissionAsync(RequestPermissionRequest r, CancellationToken ct) =>
            Task.FromResult(new RequestPermissionResponse { Outcome = new CancelledPermissionOutcome() });
    }
}
