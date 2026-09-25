using System.Text.Json;
using Acp.JsonRpc;
using Acp.Schema;
using Acp.Streaming;
using Microsoft.Extensions.Logging;

namespace Acp;

/// <summary>
/// An ACP connection from the agent's point of view. Constructed by an agent process to wrap a
/// <see cref="IMessageStream"/> (typically <see cref="NdJsonStream.FromStdio"/>). Incoming
/// requests/notifications are routed to the supplied <see cref="IAgent"/> implementation, while
/// the connection itself exposes <see cref="IClient"/>-shaped methods for the agent to call out
/// to the client.
/// </summary>
public sealed class AgentSideConnection : IClient, IAsyncDisposable
{
    private readonly IAgent _agent;
    private readonly Connection _connection;

    /// <summary>
    /// Create the connection. The factory <paramref name="agentFactory"/> receives this connection
    /// (so the agent can hold a reference to call client methods) and must return its
    /// <see cref="IAgent"/> implementation.
    /// </summary>
    public AgentSideConnection(
        Func<AgentSideConnection, IAgent> agentFactory,
        IMessageStream stream,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(agentFactory);
        ArgumentNullException.ThrowIfNull(stream);
        _agent = agentFactory(this) ?? throw new InvalidOperationException("Agent factory returned null.");
        _connection = new Connection(stream, HandleRequestAsync, HandleNotificationAsync, logger);
        _connection.Start();
    }

    /// <summary>A token that fires when the underlying connection is shutting down.</summary>
    public CancellationToken Stopping => _connection.Stopping;

    /// <summary>Resolves when the connection has fully closed (either side).</summary>
    public Task Closed => _connection.Closed;

    public ValueTask DisposeAsync() => _connection.DisposeAsync();

    // --- IClient (outbound calls from agent to client) ---

    public Task SessionUpdateAsync(SessionNotification notification, CancellationToken cancellationToken)
        => _connection.SendNotificationAsync(ClientMethods.SessionUpdate, notification, cancellationToken);

    public Task<RequestPermissionResponse> RequestPermissionAsync(RequestPermissionRequest request, CancellationToken cancellationToken)
        => _connection.SendRequestAsync<RequestPermissionResponse>(ClientMethods.SessionRequestPermission, request, cancellationToken);

    public Task<ReadTextFileResponse> ReadTextFileAsync(ReadTextFileRequest request, CancellationToken cancellationToken)
        => _connection.SendRequestAsync<ReadTextFileResponse>(ClientMethods.FsReadTextFile, request, cancellationToken);

    public Task<WriteTextFileResponse?> WriteTextFileAsync(WriteTextFileRequest request, CancellationToken cancellationToken)
        => _connection.SendRequestAsync<WriteTextFileResponse?>(ClientMethods.FsWriteTextFile, request, cancellationToken);

    /// <summary>Convenience: create a terminal and return a <see cref="TerminalHandle"/> for lifecycle management.</summary>
    public async Task<TerminalHandle> CreateTerminalHandleAsync(CreateTerminalRequest request, CancellationToken cancellationToken)
    {
        CreateTerminalResponse resp = await CreateTerminalAsync(request, cancellationToken).ConfigureAwait(false);
        return new TerminalHandle(resp.TerminalId, request.SessionId, this);
    }

    public Task<CreateTerminalResponse> CreateTerminalAsync(CreateTerminalRequest request, CancellationToken cancellationToken)
        => _connection.SendRequestAsync<CreateTerminalResponse>(ClientMethods.TerminalCreate, request, cancellationToken);

    public Task<TerminalOutputResponse> TerminalOutputAsync(TerminalOutputRequest request, CancellationToken cancellationToken)
        => _connection.SendRequestAsync<TerminalOutputResponse>(ClientMethods.TerminalOutput, request, cancellationToken);

    public Task<ReleaseTerminalResponse?> ReleaseTerminalAsync(ReleaseTerminalRequest request, CancellationToken cancellationToken)
        => _connection.SendRequestAsync<ReleaseTerminalResponse?>(ClientMethods.TerminalRelease, request, cancellationToken);

    public Task<WaitForTerminalExitResponse> WaitForTerminalExitAsync(WaitForTerminalExitRequest request, CancellationToken cancellationToken)
        => _connection.SendRequestAsync<WaitForTerminalExitResponse>(ClientMethods.TerminalWaitForExit, request, cancellationToken);

    public Task<KillTerminalResponse?> KillTerminalAsync(KillTerminalRequest request, CancellationToken cancellationToken)
        => _connection.SendRequestAsync<KillTerminalResponse?>(ClientMethods.TerminalKill, request, cancellationToken);

    public Task<JsonElement?> ExtMethodAsync(string method, JsonElement? @params, CancellationToken cancellationToken)
        => _connection.SendRequestAsync<JsonElement?>(method, @params, cancellationToken);

    public Task ExtNotificationAsync(string method, JsonElement? @params, CancellationToken cancellationToken)
        => _connection.SendNotificationAsync(method, @params, cancellationToken);

    // --- inbound dispatch (client -> agent) ---

    private async Task<object?> HandleRequestAsync(string method, JsonElement? @params, CancellationToken ct)
    {
        switch (method)
        {
            case AgentMethods.Initialize:
                return await _agent.InitializeAsync(Deserialize<InitializeRequest>(@params, method), ct).ConfigureAwait(false);
            case AgentMethods.Authenticate:
                return await _agent.AuthenticateAsync(Deserialize<AuthenticateRequest>(@params, method), ct).ConfigureAwait(false) ?? new AuthenticateResponse();
            case AgentMethods.SessionNew:
                return await _agent.NewSessionAsync(Deserialize<NewSessionRequest>(@params, method), ct).ConfigureAwait(false);
            case AgentMethods.SessionLoad:
                return await _agent.LoadSessionAsync(Deserialize<LoadSessionRequest>(@params, method), ct).ConfigureAwait(false);
            case AgentMethods.SessionResume:
                return await _agent.ResumeSessionAsync(Deserialize<ResumeSessionRequest>(@params, method), ct).ConfigureAwait(false);
            case AgentMethods.SessionList:
                return await _agent.ListSessionsAsync(Deserialize<ListSessionsRequest>(@params, method), ct).ConfigureAwait(false);
            case AgentMethods.SessionClose:
                return await _agent.CloseSessionAsync(Deserialize<CloseSessionRequest>(@params, method), ct).ConfigureAwait(false) ?? new CloseSessionResponse();
            case AgentMethods.SessionDelete:
                return await _agent.DeleteSessionAsync(Deserialize<DeleteSessionRequest>(@params, method), ct).ConfigureAwait(false) ?? new DeleteSessionResponse();
            case AgentMethods.Logout:
                return await _agent.LogoutAsync(DeserializeOrDefault<LogoutRequest>(@params, method), ct).ConfigureAwait(false) ?? new LogoutResponse();
            case AgentMethods.SessionPrompt:
                return await _agent.PromptAsync(Deserialize<PromptRequest>(@params, method), ct).ConfigureAwait(false);
            case AgentMethods.SessionSetMode:
                return await _agent.SetSessionModeAsync(Deserialize<SetSessionModeRequest>(@params, method), ct).ConfigureAwait(false) ?? new SetSessionModeResponse();
            case AgentMethods.SessionSetModel:
                return await _agent.SetSessionModelAsync(Deserialize<SetSessionModelRequest>(@params, method), ct).ConfigureAwait(false) ?? new SetSessionModelResponse();
            case AgentMethods.SessionSetConfigOption:
                return await _agent.SetSessionConfigOptionAsync(Deserialize<SetSessionConfigOptionRequest>(@params, method), ct).ConfigureAwait(false);
            default:
                return await _agent.ExtMethodAsync(method, @params, ct).ConfigureAwait(false);
        }
    }

    private async Task HandleNotificationAsync(string method, JsonElement? @params, CancellationToken ct)
    {
        switch (method)
        {
            case AgentMethods.SessionCancel:
                await _agent.CancelAsync(Deserialize<CancelNotification>(@params, method), ct).ConfigureAwait(false);
                break;
            default:
                await _agent.ExtNotificationAsync(method, @params, ct).ConfigureAwait(false);
                break;
        }
    }

    /// <summary>Like <see cref="Deserialize{T}"/>, but a missing <c>params</c> yields a default instance (for all-optional requests).</summary>
    internal static T DeserializeOrDefault<T>(JsonElement? @params, string method) where T : new()
        => @params is null || @params.Value.ValueKind == JsonValueKind.Null ? new T() : Deserialize<T>(@params, method);

    internal static T Deserialize<T>(JsonElement? @params, string method)
    {
        if (@params is null)
        {
            throw RequestErrorException.InvalidParams(additionalMessage: $"missing 'params' for method '{method}'.");
        }
        try
        {
            return @params.Value.Deserialize<T>(AcpJson.Options)
                ?? throw RequestErrorException.InvalidParams(additionalMessage: $"null params decoded as {typeof(T).Name} for method '{method}'.");
        }
        catch (JsonException ex)
        {
            throw RequestErrorException.InvalidParams(new { error = ex.Message }, $"failed to decode params for '{method}'.");
        }
    }
}
