using System.Text.Json;
using Acp.JsonRpc;
using Acp.Schema;
using Acp.Streaming;
using Microsoft.Extensions.Logging;

namespace Acp;

/// <summary>
/// An ACP connection from the client's point of view. The client constructs this to wrap a
/// stream (typically the stdio of a spawned agent subprocess). Incoming requests/notifications
/// are routed to the supplied <see cref="IClient"/>; the connection itself implements
/// <see cref="IAgent"/> for the client to call out to the agent.
/// </summary>
public sealed class ClientSideConnection : IAgent, IAsyncDisposable
{
    private readonly IClient _client;
    private readonly Connection _connection;

    /// <summary>
    /// Create the connection. The factory <paramref name="clientFactory"/> receives this connection
    /// (so the client can hold a reference to call agent methods) and must return its
    /// <see cref="IClient"/> implementation.
    /// </summary>
    public ClientSideConnection(
        Func<ClientSideConnection, IClient> clientFactory,
        IMessageStream stream,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentNullException.ThrowIfNull(stream);
        _client = clientFactory(this) ?? throw new InvalidOperationException("Client factory returned null.");
        _connection = new Connection(stream, HandleRequestAsync, HandleNotificationAsync, logger);
        _connection.Start();
    }

    /// <summary>A token that fires when the underlying connection is shutting down.</summary>
    public CancellationToken Stopping => _connection.Stopping;

    /// <summary>Resolves when the connection has fully closed (either side).</summary>
    public Task Closed => _connection.Closed;

    public ValueTask DisposeAsync() => _connection.DisposeAsync();

    // --- IAgent (outbound calls from client to agent) ---

    public Task<InitializeResponse> InitializeAsync(InitializeRequest request, CancellationToken cancellationToken)
        => _connection.SendRequestAsync<InitializeResponse>(AgentMethods.Initialize, request, cancellationToken);

    public Task<AuthenticateResponse?> AuthenticateAsync(AuthenticateRequest request, CancellationToken cancellationToken)
        => _connection.SendRequestAsync<AuthenticateResponse?>(AgentMethods.Authenticate, request, cancellationToken);

    public Task<NewSessionResponse> NewSessionAsync(NewSessionRequest request, CancellationToken cancellationToken)
        => _connection.SendRequestAsync<NewSessionResponse>(AgentMethods.SessionNew, request, cancellationToken);

    public Task<LoadSessionResponse?> LoadSessionAsync(LoadSessionRequest request, CancellationToken cancellationToken)
        => _connection.SendRequestAsync<LoadSessionResponse?>(AgentMethods.SessionLoad, request, cancellationToken);

    public Task<ResumeSessionResponse?> ResumeSessionAsync(ResumeSessionRequest request, CancellationToken cancellationToken)
        => _connection.SendRequestAsync<ResumeSessionResponse?>(AgentMethods.SessionResume, request, cancellationToken);

    public Task<ListSessionsResponse> ListSessionsAsync(ListSessionsRequest request, CancellationToken cancellationToken)
        => _connection.SendRequestAsync<ListSessionsResponse>(AgentMethods.SessionList, request, cancellationToken);

    public Task<CloseSessionResponse?> CloseSessionAsync(CloseSessionRequest request, CancellationToken cancellationToken)
        => _connection.SendRequestAsync<CloseSessionResponse?>(AgentMethods.SessionClose, request, cancellationToken);

    public Task<DeleteSessionResponse?> DeleteSessionAsync(DeleteSessionRequest request, CancellationToken cancellationToken)
        => _connection.SendRequestAsync<DeleteSessionResponse?>(AgentMethods.SessionDelete, request, cancellationToken);

    public Task<LogoutResponse?> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
        => _connection.SendRequestAsync<LogoutResponse?>(AgentMethods.Logout, request, cancellationToken);

    public Task<PromptResponse> PromptAsync(PromptRequest request, CancellationToken cancellationToken)
        => _connection.SendRequestAsync<PromptResponse>(AgentMethods.SessionPrompt, request, cancellationToken);

    public Task<SetSessionModeResponse?> SetSessionModeAsync(SetSessionModeRequest request, CancellationToken cancellationToken)
        => _connection.SendRequestAsync<SetSessionModeResponse?>(AgentMethods.SessionSetMode, request, cancellationToken);

    public Task<SetSessionModelResponse?> SetSessionModelAsync(SetSessionModelRequest request, CancellationToken cancellationToken)
        => _connection.SendRequestAsync<SetSessionModelResponse?>(AgentMethods.SessionSetModel, request, cancellationToken);

    public Task<SetSessionConfigOptionResponse> SetSessionConfigOptionAsync(SetSessionConfigOptionRequest request, CancellationToken cancellationToken)
        => _connection.SendRequestAsync<SetSessionConfigOptionResponse>(AgentMethods.SessionSetConfigOption, request, cancellationToken);

    public Task CancelAsync(CancelNotification notification, CancellationToken cancellationToken)
        => _connection.SendNotificationAsync(AgentMethods.SessionCancel, notification, cancellationToken);

    public Task<JsonElement?> ExtMethodAsync(string method, JsonElement? @params, CancellationToken cancellationToken)
        => _connection.SendRequestAsync<JsonElement?>(method, @params, cancellationToken);

    public Task ExtNotificationAsync(string method, JsonElement? @params, CancellationToken cancellationToken)
        => _connection.SendNotificationAsync(method, @params, cancellationToken);

    // --- inbound dispatch (agent -> client) ---

    private async Task<object?> HandleRequestAsync(string method, JsonElement? @params, CancellationToken ct)
    {
        switch (method)
        {
            case ClientMethods.SessionRequestPermission:
                return await _client.RequestPermissionAsync(AgentSideConnection.Deserialize<RequestPermissionRequest>(@params, method), ct).ConfigureAwait(false);
            case ClientMethods.FsReadTextFile:
                return await _client.ReadTextFileAsync(AgentSideConnection.Deserialize<ReadTextFileRequest>(@params, method), ct).ConfigureAwait(false);
            case ClientMethods.FsWriteTextFile:
                return await _client.WriteTextFileAsync(AgentSideConnection.Deserialize<WriteTextFileRequest>(@params, method), ct).ConfigureAwait(false) ?? new WriteTextFileResponse();
            case ClientMethods.TerminalCreate:
                return await _client.CreateTerminalAsync(AgentSideConnection.Deserialize<CreateTerminalRequest>(@params, method), ct).ConfigureAwait(false);
            case ClientMethods.TerminalOutput:
                return await _client.TerminalOutputAsync(AgentSideConnection.Deserialize<TerminalOutputRequest>(@params, method), ct).ConfigureAwait(false);
            case ClientMethods.TerminalRelease:
                return await _client.ReleaseTerminalAsync(AgentSideConnection.Deserialize<ReleaseTerminalRequest>(@params, method), ct).ConfigureAwait(false) ?? new ReleaseTerminalResponse();
            case ClientMethods.TerminalWaitForExit:
                return await _client.WaitForTerminalExitAsync(AgentSideConnection.Deserialize<WaitForTerminalExitRequest>(@params, method), ct).ConfigureAwait(false);
            case ClientMethods.TerminalKill:
                return await _client.KillTerminalAsync(AgentSideConnection.Deserialize<KillTerminalRequest>(@params, method), ct).ConfigureAwait(false) ?? new KillTerminalResponse();
            case ClientMethods.ElicitationCreate:
                return await _client.CreateElicitationAsync(AgentSideConnection.Deserialize<CreateElicitationRequest>(@params, method), ct).ConfigureAwait(false);
            default:
                return await _client.ExtMethodAsync(method, @params, ct).ConfigureAwait(false);
        }
    }

    private async Task HandleNotificationAsync(string method, JsonElement? @params, CancellationToken ct)
    {
        switch (method)
        {
            case ClientMethods.SessionUpdate:
                await _client.SessionUpdateAsync(AgentSideConnection.Deserialize<SessionNotification>(@params, method), ct).ConfigureAwait(false);
                break;
            case ClientMethods.ElicitationComplete:
                await _client.CompleteElicitationAsync(AgentSideConnection.Deserialize<CompleteElicitationNotification>(@params, method), ct).ConfigureAwait(false);
                break;
            default:
                await _client.ExtNotificationAsync(method, @params, ct).ConfigureAwait(false);
                break;
        }
    }
}
