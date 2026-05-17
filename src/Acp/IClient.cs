using System.Text.Json;
using Acp.Schema;

namespace Acp;

/// <summary>
/// Interface implemented by an ACP <strong>Client</strong> — typically a code editor.
/// Clients receive notifications and requests from the <strong>Agent</strong> (e.g. session
/// updates, permission requests, file system access).
/// </summary>
/// <remarks>
/// Only <see cref="SessionUpdateAsync"/> and <see cref="RequestPermissionAsync"/> are required.
/// All optional methods have default implementations that throw <c>MethodNotFound</c>.
/// </remarks>
public interface IClient
{
    /// <summary>Receives a streaming session update notification.</summary>
    Task SessionUpdateAsync(SessionNotification notification, CancellationToken cancellationToken);

    /// <summary>Asks the user for permission to perform a tool call.</summary>
    Task<RequestPermissionResponse> RequestPermissionAsync(RequestPermissionRequest request, CancellationToken cancellationToken);

    /// <summary>Reads a text file from the client's filesystem. Requires <c>fs.readTextFile</c>.</summary>
    Task<ReadTextFileResponse> ReadTextFileAsync(ReadTextFileRequest request, CancellationToken cancellationToken)
        => throw JsonRpc.RequestErrorException.MethodNotFound(Schema.ClientMethods.FsReadTextFile);

    /// <summary>Writes a text file in the client's filesystem. Requires <c>fs.writeTextFile</c>.</summary>
    Task<WriteTextFileResponse?> WriteTextFileAsync(WriteTextFileRequest request, CancellationToken cancellationToken)
        => throw JsonRpc.RequestErrorException.MethodNotFound(Schema.ClientMethods.FsWriteTextFile);

    /// <summary>Creates a terminal. Requires the client's <c>terminal</c> capability.</summary>
    Task<CreateTerminalResponse> CreateTerminalAsync(CreateTerminalRequest request, CancellationToken cancellationToken)
        => throw JsonRpc.RequestErrorException.MethodNotFound(Schema.ClientMethods.TerminalCreate);

    /// <summary>Reads current terminal output without waiting for exit.</summary>
    Task<TerminalOutputResponse> TerminalOutputAsync(TerminalOutputRequest request, CancellationToken cancellationToken)
        => throw JsonRpc.RequestErrorException.MethodNotFound(Schema.ClientMethods.TerminalOutput);

    /// <summary>Releases a terminal and frees resources.</summary>
    Task<ReleaseTerminalResponse?> ReleaseTerminalAsync(ReleaseTerminalRequest request, CancellationToken cancellationToken)
        => throw JsonRpc.RequestErrorException.MethodNotFound(Schema.ClientMethods.TerminalRelease);

    /// <summary>Waits for a terminal command to exit.</summary>
    Task<WaitForTerminalExitResponse> WaitForTerminalExitAsync(WaitForTerminalExitRequest request, CancellationToken cancellationToken)
        => throw JsonRpc.RequestErrorException.MethodNotFound(Schema.ClientMethods.TerminalWaitForExit);

    /// <summary>Kills a terminal command without releasing the terminal id.</summary>
    Task<KillTerminalResponse?> KillTerminalAsync(KillTerminalRequest request, CancellationToken cancellationToken)
        => throw JsonRpc.RequestErrorException.MethodNotFound(Schema.ClientMethods.TerminalKill);

    /// <summary>Escape hatch for custom JSON-RPC requests (names starting with <c>_</c>).</summary>
    Task<JsonElement?> ExtMethodAsync(string method, JsonElement? @params, CancellationToken cancellationToken)
        => throw JsonRpc.RequestErrorException.MethodNotFound(method);

    /// <summary>Escape hatch for custom JSON-RPC notifications. Default implementation ignores them.</summary>
    Task ExtNotificationAsync(string method, JsonElement? @params, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
