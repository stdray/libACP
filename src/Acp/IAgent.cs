using System.Text.Json;
using Acp.Schema;

namespace Acp;

/// <summary>
/// Interface implemented by an ACP <strong>Agent</strong> — the program that uses generative AI
/// to autonomously modify code. The agent is invoked by a <strong>Client</strong> (typically a
/// code editor) over a <see cref="JsonRpc.Connection"/>.
/// </summary>
/// <remarks>
/// Required methods (<see cref="InitializeAsync"/>, <see cref="NewSessionAsync"/>,
/// <see cref="PromptAsync"/>, <see cref="AuthenticateAsync"/>, <see cref="CancelAsync"/>) MUST be
/// implemented. All other methods have default implementations that throw a
/// <c>MethodNotFound</c> JSON-RPC error.
/// </remarks>
public interface IAgent
{
    /// <summary>Establishes the connection and negotiates protocol version + capabilities.</summary>
    Task<InitializeResponse> InitializeAsync(InitializeRequest request, CancellationToken cancellationToken);

    /// <summary>Authenticates using the specified method (advertised in <c>initialize</c>).</summary>
    Task<AuthenticateResponse?> AuthenticateAsync(AuthenticateRequest request, CancellationToken cancellationToken);

    /// <summary>Creates a new conversation session.</summary>
    Task<NewSessionResponse> NewSessionAsync(NewSessionRequest request, CancellationToken cancellationToken);

    /// <summary>Processes a user prompt within a session.</summary>
    Task<PromptResponse> PromptAsync(PromptRequest request, CancellationToken cancellationToken);

    /// <summary>Notification that the current prompt turn should be cancelled.</summary>
    Task CancelAsync(CancelNotification notification, CancellationToken cancellationToken);

    /// <summary>Loads an existing session and replays its conversation. Requires <c>loadSession</c> capability.</summary>
    Task<LoadSessionResponse?> LoadSessionAsync(LoadSessionRequest request, CancellationToken cancellationToken)
        => throw JsonRpc.RequestErrorException.MethodNotFound(Schema.AgentMethods.SessionLoad);

    /// <summary>Resumes an existing session without replaying. Requires <c>session.resume</c> capability.</summary>
    Task<ResumeSessionResponse?> ResumeSessionAsync(ResumeSessionRequest request, CancellationToken cancellationToken)
        => throw JsonRpc.RequestErrorException.MethodNotFound(Schema.AgentMethods.SessionResume);

    /// <summary>Lists existing sessions. Requires <c>session.list</c> capability.</summary>
    Task<ListSessionsResponse> ListSessionsAsync(ListSessionsRequest request, CancellationToken cancellationToken)
        => throw JsonRpc.RequestErrorException.MethodNotFound(Schema.AgentMethods.SessionList);

    /// <summary>Closes an active session. Requires <c>session.close</c> capability.</summary>
    Task<CloseSessionResponse?> CloseSessionAsync(CloseSessionRequest request, CancellationToken cancellationToken)
        => throw JsonRpc.RequestErrorException.MethodNotFound(Schema.AgentMethods.SessionClose);

    /// <summary>Sets the operational mode for a session.</summary>
    Task<SetSessionModeResponse?> SetSessionModeAsync(SetSessionModeRequest request, CancellationToken cancellationToken)
        => throw JsonRpc.RequestErrorException.MethodNotFound(Schema.AgentMethods.SessionSetMode);

    /// <summary>Sets the model for a session.</summary>
    Task<SetSessionModelResponse?> SetSessionModelAsync(SetSessionModelRequest request, CancellationToken cancellationToken)
        => throw JsonRpc.RequestErrorException.MethodNotFound(Schema.AgentMethods.SessionSetModel);

    /// <summary>Sets a session-level configuration option.</summary>
    Task<SetSessionConfigOptionResponse> SetSessionConfigOptionAsync(SetSessionConfigOptionRequest request, CancellationToken cancellationToken)
        => throw JsonRpc.RequestErrorException.MethodNotFound(Schema.AgentMethods.SessionSetConfigOption);

    /// <summary>
    /// Escape hatch for custom JSON-RPC requests — methods whose name starts with <c>_</c>. Default
    /// implementation responds with <c>MethodNotFound</c>.
    /// </summary>
    Task<JsonElement?> ExtMethodAsync(string method, JsonElement? @params, CancellationToken cancellationToken)
        => throw JsonRpc.RequestErrorException.MethodNotFound(method);

    /// <summary>Escape hatch for custom JSON-RPC notifications. Default implementation ignores them.</summary>
    Task ExtNotificationAsync(string method, JsonElement? @params, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
