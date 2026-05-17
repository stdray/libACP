using System.Text.Json;
using Acp;
using Acp.JsonRpc;

namespace Acp.Tests;

public class ConnectionTests
{
    [Fact]
    public async Task RoundTripsRequest_AndDeserializesResponse()
    {
        var (a, b) = PairedNdJsonStreams.Create();

        await using var server = new Connection(a,
            requestHandler: (method, _, _) =>
            {
                Assert.Equal("echo", method);
                return Task.FromResult<object?>(new { hello = "world" });
            },
            notificationHandler: (_, _, _) => Task.CompletedTask);
        server.Start();

        await using var client = new Connection(b,
            requestHandler: (_, _, _) => Task.FromResult<object?>(null),
            notificationHandler: (_, _, _) => Task.CompletedTask);
        client.Start();

        JsonElement result = await client.SendRequestAsync<JsonElement>("echo", null);
        Assert.Equal("world", result.GetProperty("hello").GetString());
    }

    [Fact]
    public async Task UnknownMethod_ResultsInMethodNotFound()
    {
        var (a, b) = PairedNdJsonStreams.Create();

        await using var server = new Connection(a,
            requestHandler: (method, _, _) => throw RequestErrorException.MethodNotFound(method),
            notificationHandler: (_, _, _) => Task.CompletedTask);
        server.Start();

        await using var client = new Connection(b,
            requestHandler: (_, _, _) => Task.FromResult<object?>(null),
            notificationHandler: (_, _, _) => Task.CompletedTask);
        client.Start();

        var ex = await Assert.ThrowsAsync<RequestErrorException>(
            () => client.SendRequestAsync<JsonElement>("does/not/exist", null));
        Assert.Equal(-32601, ex.Code);
    }

    [Fact]
    public async Task HandlerThrowingArbitraryException_BecomesInternalError()
    {
        var (a, b) = PairedNdJsonStreams.Create();
        await using var server = new Connection(a,
            requestHandler: (_, _, _) => throw new InvalidOperationException("boom"),
            notificationHandler: (_, _, _) => Task.CompletedTask);
        server.Start();
        await using var client = new Connection(b,
            requestHandler: (_, _, _) => Task.FromResult<object?>(null),
            notificationHandler: (_, _, _) => Task.CompletedTask);
        client.Start();

        var ex = await Assert.ThrowsAsync<RequestErrorException>(
            () => client.SendRequestAsync<JsonElement>("anything", null));
        Assert.Equal(-32603, ex.Code);
    }

    [Fact]
    public async Task NotificationsDeliveredToHandler()
    {
        var (a, b) = PairedNdJsonStreams.Create();
        var tcs = new TaskCompletionSource<string>();

        await using var server = new Connection(a,
            requestHandler: (_, _, _) => Task.FromResult<object?>(null),
            notificationHandler: (method, _, _) => { tcs.TrySetResult(method); return Task.CompletedTask; });
        server.Start();

        await using var client = new Connection(b,
            requestHandler: (_, _, _) => Task.FromResult<object?>(null),
            notificationHandler: (_, _, _) => Task.CompletedTask);
        client.Start();

        await client.SendNotificationAsync("ping", null);
        string method = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("ping", method);
    }

    [Fact]
    public async Task SimultaneousRequests_AreCorrelatedCorrectly()
    {
        var (a, b) = PairedNdJsonStreams.Create();

        await using var server = new Connection(a,
            requestHandler: async (method, p, _) =>
            {
                int n = p!.Value.GetProperty("n").GetInt32();
                // small async pause to interleave
                await Task.Delay(n % 5);
                return new { method, n };
            },
            notificationHandler: (_, _, _) => Task.CompletedTask);
        server.Start();

        await using var client = new Connection(b,
            requestHandler: (_, _, _) => Task.FromResult<object?>(null),
            notificationHandler: (_, _, _) => Task.CompletedTask);
        client.Start();

        Task<JsonElement>[] tasks = Enumerable.Range(0, 20)
            .Select(i => client.SendRequestAsync<JsonElement>("m", new { n = i }))
            .ToArray();
        JsonElement[] results = await Task.WhenAll(tasks);
        for (int i = 0; i < results.Length; i++)
        {
            Assert.Equal(i, results[i].GetProperty("n").GetInt32());
        }
    }

    [Fact]
    public async Task LocalCancellation_DoesNotPoisonOtherRequests()
    {
        var (a, b) = PairedNdJsonStreams.Create();
        var release = new TaskCompletionSource<bool>();

        await using var server = new Connection(a,
            requestHandler: async (method, _, _) =>
            {
                if (method == "slow") await release.Task;
                return new { ok = true };
            },
            notificationHandler: (_, _, _) => Task.CompletedTask);
        server.Start();

        await using var client = new Connection(b,
            requestHandler: (_, _, _) => Task.FromResult<object?>(null),
            notificationHandler: (_, _, _) => Task.CompletedTask);
        client.Start();

        using var cts = new CancellationTokenSource();
        Task<JsonElement> slow = client.SendRequestAsync<JsonElement>("slow", null, cts.Token);
        Task<JsonElement> fast = client.SendRequestAsync<JsonElement>("fast", null);

        cts.Cancel();
        await Assert.ThrowsAsync<TaskCanceledException>(() => slow);

        JsonElement r = await fast.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(r.GetProperty("ok").GetBoolean());

        release.SetResult(true);
    }
}
