using System.Text.Json;
using Acp.JsonRpc;

namespace Acp.Tests;

public class ConnectionConcurrencyDiagnostics
{
    [Fact]
    public async Task Single_RoundTripAfterRoundTrip_Works()
    {
        var (a, b) = PairedNdJsonStreams.Create();
        await using var server = new Connection(a,
            requestHandler: (m, p, _) => Task.FromResult<object?>(new { ok = true }),
            notificationHandler: (_, _, _) => Task.CompletedTask);
        server.Start();
        await using var client = new Connection(b,
            requestHandler: (_, _, _) => Task.FromResult<object?>(null),
            notificationHandler: (_, _, _) => Task.CompletedTask);
        client.Start();

        for (int i = 0; i < 5; i++)
        {
            JsonElement r = await client.SendRequestAsync<JsonElement>("m", new { n = i })
                .WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(r.GetProperty("ok").GetBoolean());
        }
    }

    [Fact]
    public async Task Parallel_TwoRequests_Works()
    {
        var (a, b) = PairedNdJsonStreams.Create();
        await using var server = new Connection(a,
            requestHandler: (m, p, _) => Task.FromResult<object?>(new { ok = true }),
            notificationHandler: (_, _, _) => Task.CompletedTask);
        server.Start();
        await using var client = new Connection(b,
            requestHandler: (_, _, _) => Task.FromResult<object?>(null),
            notificationHandler: (_, _, _) => Task.CompletedTask);
        client.Start();

        Task<JsonElement> t1 = client.SendRequestAsync<JsonElement>("m", new { n = 0 });
        Task<JsonElement> t2 = client.SendRequestAsync<JsonElement>("m", new { n = 1 });
        await Task.WhenAll(t1, t2).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    public async Task Parallel_NRequests_Works(int n)
    {
        var (a, b) = PairedNdJsonStreams.Create();
        await using var server = new Connection(a,
            requestHandler: (_, _, _) => Task.FromResult<object?>(new { ok = true }),
            notificationHandler: (_, _, _) => Task.CompletedTask);
        server.Start();
        await using var client = new Connection(b,
            requestHandler: (_, _, _) => Task.FromResult<object?>(null),
            notificationHandler: (_, _, _) => Task.CompletedTask);
        client.Start();

        Task<JsonElement>[] tasks = Enumerable.Range(0, n)
            .Select(i => client.SendRequestAsync<JsonElement>("m", new { n = i }))
            .ToArray();
        await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(10));
    }
}
