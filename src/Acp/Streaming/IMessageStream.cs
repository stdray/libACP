using Acp.JsonRpc;

namespace Acp.Streaming;

/// <summary>
/// A bidirectional JSON-RPC message stream. Implementations are responsible for framing, encoding,
/// and lifecycle. The <see cref="Acp.JsonRpc.Connection"/> class consumes one of these to drive
/// the protocol.
/// </summary>
public interface IMessageStream : IAsyncDisposable
{
    /// <summary>
    /// Read the next message from the peer. Returns <c>null</c> when the input is closed cleanly.
    /// </summary>
    /// <param name="cancellationToken">Token that, when cancelled, abandons the read.</param>
    /// <exception cref="JsonRpc.RequestErrorException">
    /// Thrown with code <c>-32700</c> when a malformed JSON message is received. The connection
    /// will reply with a parse error response when possible.
    /// </exception>
    Task<JsonRpcMessage?> ReadAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Write a message to the peer. Implementations must serialize concurrent calls so messages do
    /// not interleave on the wire.
    /// </summary>
    Task WriteAsync(JsonRpcMessage message, CancellationToken cancellationToken);
}
