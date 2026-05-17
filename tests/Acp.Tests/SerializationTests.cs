using System.Text.Json;
using Acp;
using Acp.Schema;

namespace Acp.Tests;

public class SerializationTests
{
    private static T RoundTrip<T>(T value)
    {
        string json = JsonSerializer.Serialize(value, AcpJson.Options);
        T? back = JsonSerializer.Deserialize<T>(json, AcpJson.Options);
        Assert.NotNull(back);
        return back!;
    }

    [Fact]
    public void TypedIds_RoundTrip()
    {
        var sid = new SessionId("sess-1");
        string json = JsonSerializer.Serialize(sid, AcpJson.Options);
        Assert.Equal("\"sess-1\"", json);
        Assert.Equal(sid, JsonSerializer.Deserialize<SessionId>(json, AcpJson.Options));
    }

    [Fact]
    public void StopReason_SerializesAsSnakeCase()
    {
        Assert.Equal("\"end_turn\"", JsonSerializer.Serialize(StopReason.EndTurn, AcpJson.Options));
        Assert.Equal("\"max_tokens\"", JsonSerializer.Serialize(StopReason.MaxTokens, AcpJson.Options));
        Assert.Equal("\"max_turn_requests\"", JsonSerializer.Serialize(StopReason.MaxTurnRequests, AcpJson.Options));
        Assert.Equal(StopReason.MaxTurnRequests, JsonSerializer.Deserialize<StopReason>("\"max_turn_requests\"", AcpJson.Options));
    }

    [Fact]
    public void ContentBlock_Text_RoundTripsViaPolymorphicConverter()
    {
        ContentBlock block = new TextContent { Text = "hello" };
        string json = JsonSerializer.Serialize(block, AcpJson.Options);
        Assert.Contains("\"type\":\"text\"", json);
        Assert.Contains("\"text\":\"hello\"", json);

        ContentBlock back = JsonSerializer.Deserialize<ContentBlock>(json, AcpJson.Options)!;
        var t = Assert.IsType<TextContent>(back);
        Assert.Equal("hello", t.Text);
    }

    [Fact]
    public void ContentBlock_Image_RoundTrips()
    {
        ContentBlock block = new ImageContent { Data = "abc", MimeType = "image/png" };
        string json = JsonSerializer.Serialize(block, AcpJson.Options);
        Assert.Contains("\"type\":\"image\"", json);

        var back = (ImageContent)JsonSerializer.Deserialize<ContentBlock>(json, AcpJson.Options)!;
        Assert.Equal("abc", back.Data);
        Assert.Equal("image/png", back.MimeType);
    }

    [Fact]
    public void ContentBlock_EmbeddedResource_TextVariant_RoundTrips()
    {
        ContentBlock block = new EmbeddedResourceContent
        {
            Resource = new TextResourceContents { Uri = "file:///x.md", Text = "# hi" },
        };
        string json = JsonSerializer.Serialize(block, AcpJson.Options);
        Assert.Contains("\"type\":\"resource\"", json);
        Assert.Contains("\"text\":\"# hi\"", json);

        var back = (EmbeddedResourceContent)JsonSerializer.Deserialize<ContentBlock>(json, AcpJson.Options)!;
        var text = Assert.IsType<TextResourceContents>(back.Resource);
        Assert.Equal("# hi", text.Text);
    }

    [Fact]
    public void ContentBlock_EmbeddedResource_BlobVariant_RoundTrips()
    {
        ContentBlock block = new EmbeddedResourceContent
        {
            Resource = new BlobResourceContents { Uri = "file:///x.bin", Blob = "AAA=" },
        };
        string json = JsonSerializer.Serialize(block, AcpJson.Options);
        var back = (EmbeddedResourceContent)JsonSerializer.Deserialize<ContentBlock>(json, AcpJson.Options)!;
        var blob = Assert.IsType<BlobResourceContents>(back.Resource);
        Assert.Equal("AAA=", blob.Blob);
    }

    [Fact]
    public void SessionUpdate_AgentMessageChunk_RoundTrips()
    {
        SessionUpdate u = new AgentMessageChunk { Content = new TextContent { Text = "hi" } };
        string json = JsonSerializer.Serialize(u, AcpJson.Options);
        Assert.Contains("\"sessionUpdate\":\"agent_message_chunk\"", json);
        Assert.Contains("\"content\"", json);
        Assert.Contains("\"type\":\"text\"", json);

        SessionUpdate back = JsonSerializer.Deserialize<SessionUpdate>(json, AcpJson.Options)!;
        var amc = Assert.IsType<AgentMessageChunk>(back);
        Assert.IsType<TextContent>(amc.Content);
    }

    [Fact]
    public void SessionUpdate_ToolCallStart_RoundTripsWithKind()
    {
        SessionUpdate u = new ToolCallStartUpdate
        {
            ToolCallId = new ToolCallId("tc1"),
            Title = "list files",
            Kind = ToolKind.Read,
            Status = ToolCallStatus.Pending,
        };
        string json = JsonSerializer.Serialize(u, AcpJson.Options);
        Assert.Contains("\"sessionUpdate\":\"tool_call\"", json);
        Assert.Contains("\"kind\":\"read\"", json);
        Assert.Contains("\"status\":\"pending\"", json);

        var back = (ToolCallStartUpdate)JsonSerializer.Deserialize<SessionUpdate>(json, AcpJson.Options)!;
        Assert.Equal("tc1", (string)back.ToolCallId);
        Assert.Equal(ToolKind.Read, back.Kind);
        Assert.Equal(ToolCallStatus.Pending, back.Status);
    }

    [Fact]
    public void RequestPermissionOutcome_Selected_RoundTrips()
    {
        RequestPermissionOutcome outcome = new SelectedPermissionOutcome { OptionId = new PermissionOptionId("allow") };
        string json = JsonSerializer.Serialize(outcome, AcpJson.Options);
        Assert.Contains("\"outcome\":\"selected\"", json);
        Assert.Contains("\"optionId\":\"allow\"", json);

        var back = (SelectedPermissionOutcome)JsonSerializer.Deserialize<RequestPermissionOutcome>(json, AcpJson.Options)!;
        Assert.Equal("allow", (string)back.OptionId);
    }

    [Fact]
    public void RequestPermissionOutcome_Cancelled_RoundTrips()
    {
        RequestPermissionOutcome outcome = new CancelledPermissionOutcome();
        string json = JsonSerializer.Serialize(outcome, AcpJson.Options);
        Assert.Equal("{\"outcome\":\"cancelled\"}", json);
        Assert.IsType<CancelledPermissionOutcome>(
            JsonSerializer.Deserialize<RequestPermissionOutcome>(json, AcpJson.Options));
    }

    [Fact]
    public void McpServer_Stdio_ReadsLegacyFormWithoutType()
    {
        // legacy stdio servers have no "type" field
        const string legacy = "{\"name\":\"x\",\"command\":\"node\",\"args\":[\"a.js\"]}";
        McpServer parsed = JsonSerializer.Deserialize<McpServer>(legacy, AcpJson.Options)!;
        var stdio = Assert.IsType<McpServerStdio>(parsed);
        Assert.Equal("node", stdio.Command);
        Assert.Equal(new[] { "a.js" }, stdio.Args);
    }

    [Fact]
    public void McpServer_Http_RoundTrips()
    {
        McpServer s = new McpServerHttp { Name = "h", Url = "https://x/y" };
        string json = JsonSerializer.Serialize(s, AcpJson.Options);
        Assert.Contains("\"type\":\"http\"", json);
        var back = (McpServerHttp)JsonSerializer.Deserialize<McpServer>(json, AcpJson.Options)!;
        Assert.Equal("https://x/y", back.Url);
    }

    [Fact]
    public void ToolCallContent_Diff_RoundTrips()
    {
        ToolCallContent c = new ToolCallContentDiff { Path = "a.txt", OldText = "x", NewText = "y" };
        string json = JsonSerializer.Serialize(c, AcpJson.Options);
        Assert.Contains("\"type\":\"diff\"", json);
        var back = (ToolCallContentDiff)JsonSerializer.Deserialize<ToolCallContent>(json, AcpJson.Options)!;
        Assert.Equal("a.txt", back.Path);
        Assert.Equal("y", back.NewText);
    }

    [Fact]
    public void Meta_PreservesArbitraryKeys()
    {
        var p = new InitializeRequest
        {
            ProtocolVersion = 1,
            ClientInfo = new Implementation { Name = "x" },
            Meta = new Meta { ["custom.key"] = JsonSerializer.SerializeToElement(42) },
        };
        string json = JsonSerializer.Serialize(p, AcpJson.Options);
        Assert.Contains("\"_meta\":{\"custom.key\":42}", json);

        InitializeRequest back = JsonSerializer.Deserialize<InitializeRequest>(json, AcpJson.Options)!;
        Assert.NotNull(back.Meta);
        Assert.Equal(42, back.Meta!["custom.key"].GetInt32());
    }

    [Fact]
    public void NullValuedPropertiesAreOmitted()
    {
        var resp = new AuthenticateResponse();
        string json = JsonSerializer.Serialize(resp, AcpJson.Options);
        Assert.Equal("{}", json);
    }

    [Fact]
    public void ProtocolVersionConstantIsOne()
    {
        Assert.Equal(1, Protocol.Version);
    }

    [Fact]
    public void NewSessionRequest_RoundTripsMcpServers()
    {
        var req = new NewSessionRequest
        {
            Cwd = "/work",
            McpServers = new McpServer[]
            {
                new McpServerStdio { Name = "s1", Command = "node", Args = new[] { "a.js" } },
                new McpServerHttp { Name = "s2", Url = "https://x/y" },
            },
        };
        string json = JsonSerializer.Serialize(req, AcpJson.Options);
        var back = JsonSerializer.Deserialize<NewSessionRequest>(json, AcpJson.Options)!;
        Assert.Equal(2, back.McpServers.Count);
        Assert.IsType<McpServerStdio>(back.McpServers[0]);
        Assert.IsType<McpServerHttp>(back.McpServers[1]);
    }

    [Fact]
    public void SessionNotification_WithToolCallUpdate_RoundTrips()
    {
        var notif = new SessionNotification
        {
            SessionId = new SessionId("s1"),
            Update = new ToolCallUpdateUpdate
            {
                ToolCallId = new ToolCallId("tc1"),
                Status = ToolCallStatus.Completed,
                Content = new ToolCallContent[]
                {
                    new ToolCallContentBlock { Content = new TextContent { Text = "done" } },
                },
            },
        };
        string json = JsonSerializer.Serialize(notif, AcpJson.Options);
        var back = JsonSerializer.Deserialize<SessionNotification>(json, AcpJson.Options)!;
        Assert.Equal("s1", (string)back.SessionId);
        var u = Assert.IsType<ToolCallUpdateUpdate>(back.Update);
        Assert.Equal(ToolCallStatus.Completed, u.Status);
        Assert.NotNull(u.Content);
        Assert.Single(u.Content!);
    }
}
