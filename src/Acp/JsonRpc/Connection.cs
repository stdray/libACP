using System.Collections.Concurrent;
using System.Text.Json;
using Acp.Streaming;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Acp.JsonRpc;

/// <summary>
/// Handler invoked when a peer sends a JSON-RPC request. The returned object is serialized as the
/// <c>result</c>; throwing <see cref="RequestErrorException"/> produces a JSON-RPC error response;
/// any other exception is wrapped as <c>-32603</c> internal error.
/// </summary>
public delegate Task<object?> RequestHandler(string method, JsonElement? @params, CancellationToken cancellationToken);

/// <summary>Handler invoked when a peer sends a JSON-RPC notification.</summary>
public delegate Task NotificationHandler(string method, JsonElement? @params, CancellationToken cancellationToken);

/// <summary>
/// A JSON-RPC 2.0 connection. Owns one <see cref="IMessageStream"/>, runs a background read loop,
/// dispatches incoming requests/notifications to the supplied handlers, and correlates outgoing
/// requests with their responses.
/// </summary>
/// <remarks>
/// Implements ACP's <c>$/cancel_request</c> in both directions: cancelling the token passed to
/// <see cref="SendRequestAsync{TResponse}"/> notifies the peer, and an incoming
/// <c>$/cancel_request</c> cancels the token handed to the matching request handler. A handler that
/// then throws <see cref="OperationCanceledException"/> is answered with <c>-32800</c>.
/// </remarks>
public sealed class Connection : IAsyncDisposable
{
    private readonly IMessageStream _stream;
    private readonly RequestHandler _requestHandler;
    private readonly NotificationHandler _notificationHandler;
    private readonly ILogger _logger;

    private readonly ConcurrentDictionary<RequestIdKey, TaskCompletionSource<JsonElement?>> _pending = new();
    private readonly ConcurrentDictionary<RequestIdKey, CancellationTokenSource> _inbound = new();
    private readonly CancellationTokenSource _shutdown = new();
    private readonly TaskCompletionSource<object?> _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private long _nextId;
    private Task? _receiveLoop;
    private int _disposed;

    public Connection(
        IMessageStream stream,
        RequestHandler requestHandler,
        NotificationHandler notificationHandler,
        ILogger? logger = null)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _requestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));
        _notificationHandler = notificationHandler ?? throw new ArgumentNullException(nameof(notificationHandler));
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Begin the background receive loop. Must be called exactly once before sending.</summary>
    public void Start()
    {
        if (_receiveLoop is not null) throw new InvalidOperationException("Connection already started.");
        _receiveLoop = Task.Run(ReceiveLoopAsync);
    }

    /// <summary>A token that fires when the connection is shutting down.</summary>
    public CancellationToken Stopping => _shutdown.Token;

    /// <summary>Resolves when the connection has fully closed (either side).</summary>
    public Task Closed => _closed.Task;

    /// <summary>
    /// Send a JSON-RPC request to the peer and await the typed response.
    /// </summary>
    public async Task<TResponse> SendRequestAsync<TResponse>(
        string method,
        object? @params,
        CancellationToken cancellationToken = default)
    {
        ThrowIfClosed();

        long id = Interlocked.Increment(ref _nextId);
        RequestId rid = RequestId.FromNumber(id);
        var key = new RequestIdKey(rid);
        var tcs = new TaskCompletionSource<JsonElement?>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pending.TryAdd(key, tcs))
        {
            throw new InvalidOperationException("Request id collision (this should not happen).");
        }

        var msg = new JsonRpcMessage
        {
            JsonRpc = "2.0",
            Id = rid,
            HasId = true,
            Method = method,
            Params = @params is null ? null : JsonSerializer.SerializeToElement(@params, AcpJson.Options),
        };

        try
        {
            await _stream.WriteAsync(msg, _shutdown.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _pending.TryRemove(key, out _);
            CloseInternal(ex);
            throw;
        }

        using CancellationTokenRegistration ctr = cancellationToken.Register(static state =>
        {
            var (self, k) = ((Connection, RequestIdKey))state!;
            if (self._pending.TryRemove(k, out var pending))
            {
                pending.TrySetCanceled();
                self.NotifyPeerCancelled(k.Id);
            }
        }, (this, key));

        JsonElement? result = await tcs.Task.ConfigureAwait(false);
        if (result is null || result.Value.ValueKind == JsonValueKind.Null)
        {
            return default!;
        }
        try
        {
            return result.Value.Deserialize<TResponse>(AcpJson.Options)!;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize response for method '{method}' as {typeof(TResponse).Name}: {ex.Message}", ex);
        }
    }

    /// <summary>Send a JSON-RPC notification to the peer (no response).</summary>
    public async Task SendNotificationAsync(
        string method,
        object? @params,
        CancellationToken cancellationToken = default)
    {
        ThrowIfClosed();
        var msg = new JsonRpcMessage
        {
            JsonRpc = "2.0",
            Method = method,
            Params = @params is null ? null : JsonSerializer.SerializeToElement(@params, AcpJson.Options),
        };
        try
        {
            await _stream.WriteAsync(msg, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            CloseInternal(ex);
            throw;
        }
    }

    private void NotifyPeerCancelled(RequestId id)
    {
        if (_shutdown.IsCancellationRequested) return;
        _ = SendCancelRequestAsync(id);
    }

    private async Task SendCancelRequestAsync(RequestId id)
    {
        try
        {
            await SendNotificationAsync(Schema.ProtocolMethods.CancelRequest, new CancelRequestParams { RequestId = id }, _shutdown.Token)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to send $/cancel_request for {Id}.", id);
        }
    }

    private async Task ReceiveLoopAsync()
    {
        Exception? closeReason = null;
        try
        {
            while (!_shutdown.IsCancellationRequested)
            {
                JsonRpcMessage? msg;
                try
                {
                    msg = await _stream.ReadAsync(_shutdown.Token).ConfigureAwait(false);
                }
                catch (RequestErrorException parseError)
                {
                    _logger.LogWarning(parseError, "Failed to parse incoming JSON-RPC message.");
                    try
                    {
                        await _stream.WriteAsync(new JsonRpcMessage
                        {
                            JsonRpc = "2.0",
                            Id = RequestId.Null(),
                            HasId = true,
                            Error = parseError.ToJsonRpcError(),
                        }, _shutdown.Token).ConfigureAwait(false);
                    }
                    catch
                    {
                        // ignore secondary write failures
                    }
                    continue;
                }
                catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
                {
                    break;
                }

                if (msg is null)
                {
                    break;
                }

                _ = ProcessMessageAsync(msg);
            }
        }
        catch (Exception ex)
        {
            closeReason = ex;
            _logger.LogError(ex, "ACP receive loop terminated with an error.");
        }
        finally
        {
            CloseInternal(closeReason);
        }
    }

    private async Task ProcessMessageAsync(JsonRpcMessage msg)
    {
        try
        {
            if (msg.IsRequest)
            {
                await HandleRequestAsync(msg).ConfigureAwait(false);
            }
            else if (msg.IsNotification)
            {
                await HandleNotificationAsync(msg).ConfigureAwait(false);
            }
            else if (msg.IsResponse)
            {
                HandleResponse(msg);
            }
            else
            {
                _logger.LogWarning("Received malformed JSON-RPC message envelope.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while dispatching JSON-RPC message.");
        }
    }

    private async Task HandleRequestAsync(JsonRpcMessage msg)
    {
        // Registered before the first await so a $/cancel_request read right after this request finds it.
        var key = new RequestIdKey(msg.Id);
        using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
        bool tracked = _inbound.TryAdd(key, requestCts);

        JsonRpcMessage response;
        try
        {
            object? result = await _requestHandler(msg.Method!, msg.Params, requestCts.Token).ConfigureAwait(false);
            JsonElement resultElement = result switch
            {
                null => JsonSerializer.SerializeToElement<object?>(null, AcpJson.Options),
                JsonElement el => el,
                _ => JsonSerializer.SerializeToElement(result, result.GetType(), AcpJson.Options),
            };
            response = new JsonRpcMessage
            {
                JsonRpc = "2.0",
                Id = msg.Id,
                HasId = true,
                Result = resultElement,
            };
        }
        catch (RequestErrorException reqEx)
        {
            response = new JsonRpcMessage
            {
                JsonRpc = "2.0",
                Id = msg.Id,
                HasId = true,
                Error = reqEx.ToJsonRpcError(),
            };
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
            return;
        }
        catch (OperationCanceledException) when (requestCts.IsCancellationRequested)
        {
            response = new JsonRpcMessage
            {
                JsonRpc = "2.0",
                Id = msg.Id,
                HasId = true,
                Error = RequestErrorException.RequestCancelled().ToJsonRpcError(),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handler for method '{Method}' threw an unexpected exception.", msg.Method);
            response = new JsonRpcMessage
            {
                JsonRpc = "2.0",
                Id = msg.Id,
                HasId = true,
                Error = RequestErrorException.InternalError(new { details = ex.Message }).ToJsonRpcError(),
            };
        }

        finally
        {
            if (tracked) _inbound.TryRemove(key, out _);
        }

        try
        {
            await _stream.WriteAsync(response, _shutdown.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            CloseInternal(ex);
        }
    }

    private void HandleCancelRequest(JsonElement? @params)
    {
        RequestId id;
        try
        {
            id = @params?.Deserialize<CancelRequestParams>(AcpJson.Options)?.RequestId ?? RequestId.None;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Malformed $/cancel_request params.");
            return;
        }
        if (_inbound.TryGetValue(new RequestIdKey(id), out var cts))
        {
            try { cts.Cancel(); } catch (ObjectDisposedException) { /* request already finished */ }
        }
    }

    private async Task HandleNotificationAsync(JsonRpcMessage msg)
    {
        if (msg.Method == Schema.ProtocolMethods.CancelRequest)
        {
            HandleCancelRequest(msg.Params);
            return;
        }
        try
        {
            await _notificationHandler(msg.Method!, msg.Params, _shutdown.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
            // expected during shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification handler for '{Method}' threw an exception.", msg.Method);
        }
    }

    private void HandleResponse(JsonRpcMessage msg)
    {
        var key = new RequestIdKey(msg.Id);
        if (!_pending.TryRemove(key, out var pending))
        {
            _logger.LogWarning("Received response for unknown request id {Id}.", msg.Id);
            return;
        }

        if (msg.Error is { } err)
        {
            pending.TrySetException(new RequestErrorException(err.Code, err.Message, err.Data));
        }
        else
        {
            pending.TrySetResult(msg.Result);
        }
    }

    private void ThrowIfClosed()
    {
        if (_shutdown.IsCancellationRequested)
        {
            throw new InvalidOperationException("ACP connection is closed.");
        }
    }

    private void CloseInternal(Exception? reason)
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        try { _shutdown.Cancel(); } catch { /* ignore */ }

        Exception fault = reason ?? new InvalidOperationException("ACP connection closed.");
        foreach (var pending in _pending.Values)
        {
            pending.TrySetException(fault);
        }
        _pending.Clear();
        _closed.TrySetResult(null);
    }

    public async ValueTask DisposeAsync()
    {
        CloseInternal(null);
        if (_receiveLoop is not null)
        {
            try { await _receiveLoop.ConfigureAwait(false); } catch { /* ignore */ }
        }
        try { await _stream.DisposeAsync().ConfigureAwait(false); } catch { /* ignore */ }
        _shutdown.Dispose();
    }

    private readonly record struct RequestIdKey(RequestId Id);

    private sealed record CancelRequestParams
    {
        [System.Text.Json.Serialization.JsonPropertyName("requestId")]
        public RequestId RequestId { get; init; }

        [System.Text.Json.Serialization.JsonPropertyName("_meta")]
        public Schema.Meta? Meta { get; init; }
    }
}
