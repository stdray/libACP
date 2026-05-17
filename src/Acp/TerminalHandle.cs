using Acp.Schema;

namespace Acp;

/// <summary>
/// Convenience handle for a terminal created via <see cref="AgentSideConnection.CreateTerminalHandleAsync"/>.
/// Supports getting current output, waiting for exit, killing the command, and releasing the
/// terminal. Implements <see cref="IAsyncDisposable"/> so it can be used with <c>await using</c>
/// to ensure the terminal is released.
/// </summary>
public sealed class TerminalHandle : IAsyncDisposable
{
    private readonly AgentSideConnection _connection;
    private int _released;

    /// <summary>The terminal id assigned by the client.</summary>
    public TerminalId TerminalId { get; }

    /// <summary>The session this terminal belongs to.</summary>
    public SessionId SessionId { get; }

    public TerminalHandle(TerminalId terminalId, SessionId sessionId, AgentSideConnection connection)
    {
        TerminalId = terminalId;
        SessionId = sessionId;
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <summary>Get the current terminal output and exit status (if exited).</summary>
    public Task<TerminalOutputResponse> GetCurrentOutputAsync(CancellationToken cancellationToken = default) =>
        _connection.TerminalOutputAsync(new TerminalOutputRequest
        {
            SessionId = SessionId,
            TerminalId = TerminalId,
        }, cancellationToken);

    /// <summary>Wait for the terminal command to exit.</summary>
    public Task<WaitForTerminalExitResponse> WaitForExitAsync(CancellationToken cancellationToken = default) =>
        _connection.WaitForTerminalExitAsync(new WaitForTerminalExitRequest
        {
            SessionId = SessionId,
            TerminalId = TerminalId,
        }, cancellationToken);

    /// <summary>Kill the running command without releasing the terminal id.</summary>
    public Task KillAsync(CancellationToken cancellationToken = default) =>
        _connection.KillTerminalAsync(new KillTerminalRequest
        {
            SessionId = SessionId,
            TerminalId = TerminalId,
        }, cancellationToken);

    /// <summary>Release the terminal and free associated resources. Idempotent.</summary>
    public async Task ReleaseAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _released, 1) != 0) return;
        await _connection.ReleaseTerminalAsync(new ReleaseTerminalRequest
        {
            SessionId = SessionId,
            TerminalId = TerminalId,
        }, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        return _released != 0
            ? ValueTask.CompletedTask
            : new ValueTask(ReleaseAsync(CancellationToken.None));
    }
}
