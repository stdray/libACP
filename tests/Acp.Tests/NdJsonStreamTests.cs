using System.Text;
using System.Text.Json;
using Acp;
using Acp.JsonRpc;
using Acp.Streaming;

namespace Acp.Tests;

public class NdJsonStreamTests
{
    private static NdJsonStream Reader(string text, int maxLineLength = NdJsonStream.DefaultMaxLineLength)
    {
        var input = new MemoryStream(Encoding.UTF8.GetBytes(text));
        var output = new MemoryStream();
        return new NdJsonStream(input, output, leaveOpen: false, maxLineLength);
    }

    [Fact]
    public async Task ReadsRequest_LineFramed()
    {
        await using var stream = Reader("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"hi\",\"params\":{}}\n");
        JsonRpcMessage? msg = await stream.ReadAsync(CancellationToken.None);
        Assert.NotNull(msg);
        Assert.True(msg!.IsRequest);
        Assert.Equal("hi", msg.Method);
        Assert.Equal(1L, msg.Id.AsNumber());
    }

    [Fact]
    public async Task ReadsNotification_NoId()
    {
        await using var stream = Reader("{\"jsonrpc\":\"2.0\",\"method\":\"ping\"}\n");
        JsonRpcMessage? msg = await stream.ReadAsync(CancellationToken.None);
        Assert.NotNull(msg);
        Assert.True(msg!.IsNotification);
    }

    [Fact]
    public async Task ReadsResponse_NoMethod()
    {
        await using var stream = Reader("{\"jsonrpc\":\"2.0\",\"id\":7,\"result\":{}}\n");
        JsonRpcMessage? msg = await stream.ReadAsync(CancellationToken.None);
        Assert.NotNull(msg);
        Assert.True(msg!.IsResponse);
    }

    [Fact]
    public async Task TolratesCrlfLineEndings()
    {
        await using var stream = Reader("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"hi\"}\r\n");
        JsonRpcMessage? msg = await stream.ReadAsync(CancellationToken.None);
        Assert.NotNull(msg);
        Assert.Equal("hi", msg!.Method);
    }

    [Fact]
    public async Task SkipsBlankLines()
    {
        await using var stream = Reader("\n\n{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"hi\"}\n");
        JsonRpcMessage? msg = await stream.ReadAsync(CancellationToken.None);
        Assert.NotNull(msg);
        Assert.Equal("hi", msg!.Method);
    }

    [Fact]
    public async Task ReturnsNullOnEof()
    {
        await using var stream = Reader("");
        Assert.Null(await stream.ReadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ReadsTrailingMessageWithoutNewline()
    {
        await using var stream = Reader("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"hi\"}");
        JsonRpcMessage? msg = await stream.ReadAsync(CancellationToken.None);
        Assert.NotNull(msg);
        Assert.Equal("hi", msg!.Method);
        Assert.Null(await stream.ReadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ParseError_ThrowsRequestErrorException_NotEofLike()
    {
        await using var stream = Reader("not json\n");
        var ex = await Assert.ThrowsAsync<RequestErrorException>(() =>
            stream.ReadAsync(CancellationToken.None));
        Assert.Equal(-32700, ex.Code);
    }

    [Fact]
    public async Task EnforcesMaxLineLength()
    {
        // 32 bytes of garbage with no newline, against a 16-byte max.
        await using var stream = Reader(new string('a', 32), maxLineLength: 16);
        var ex = await Assert.ThrowsAsync<RequestErrorException>(() =>
            stream.ReadAsync(CancellationToken.None));
        Assert.Equal(-32700, ex.Code);
    }

    [Fact]
    public async Task WriteAsync_AppendsNewline_AndIsRecoverableByPeer()
    {
        var output = new MemoryStream();
        await using (var s = new NdJsonStream(new MemoryStream(), output, leaveOpen: true))
        {
            await s.WriteAsync(new JsonRpcMessage
            {
                JsonRpc = "2.0",
                Method = "ping",
            }, CancellationToken.None);
        }
        string text = Encoding.UTF8.GetString(output.ToArray());
        Assert.EndsWith("\n", text);
        // Peer should be able to read it back.
        await using var reader = new NdJsonStream(new MemoryStream(output.ToArray()), new MemoryStream());
        var msg = await reader.ReadAsync(CancellationToken.None);
        Assert.NotNull(msg);
        Assert.Equal("ping", msg!.Method);
    }

    [Fact]
    public async Task PreservesParamsAndResultClones()
    {
        // Confirm JsonElement payloads survive after the source document is gone.
        await using var stream = Reader(
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"x\",\"params\":{\"a\":[1,2,3]}}\n");
        var msg = await stream.ReadAsync(CancellationToken.None);
        // No exception means JsonElement was cloned.
        Assert.True(msg!.Params.HasValue);
        Assert.Equal(JsonValueKind.Object, msg.Params!.Value.ValueKind);
        Assert.Equal(3, msg.Params.Value.GetProperty("a").GetArrayLength());
    }
}
