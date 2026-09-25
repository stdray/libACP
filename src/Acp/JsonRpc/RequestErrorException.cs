using System.Text.Json;

namespace Acp.JsonRpc;

/// <summary>
/// An exception representing a JSON-RPC 2.0 error. When thrown from a request handler, the
/// <see cref="Acp.JsonRpc.Connection"/> serializes it into a JSON-RPC error response.
/// </summary>
public sealed class RequestErrorException : Exception
{
    /// <summary>The JSON-RPC error code.</summary>
    public int Code { get; }

    /// <summary>Optional structured data accompanying the error.</summary>
    public JsonElement? ErrorData { get; }

    public RequestErrorException(int code, string message, JsonElement? data = null) : base(message)
    {
        Code = code;
        ErrorData = data;
    }

    /// <summary>Convert this exception into its JSON-RPC <see cref="JsonRpcError"/> shape.</summary>
    public JsonRpcError ToJsonRpcError() => new()
    {
        Code = Code,
        Message = Message,
        Data = ErrorData,
    };

    private static JsonElement? Wrap(object? data)
    {
        if (data is null) return null;
        if (data is JsonElement el) return el.Clone();
        return JsonSerializer.SerializeToElement(data, AcpJson.Options);
    }

    /// <summary>JSON-RPC <c>-32700</c> Parse error.</summary>
    public static RequestErrorException ParseError(object? data = null, string? additionalMessage = null) =>
        new(-32700, additionalMessage is null ? "Parse error" : $"Parse error: {additionalMessage}", Wrap(data));

    /// <summary>JSON-RPC <c>-32600</c> Invalid request.</summary>
    public static RequestErrorException InvalidRequest(object? data = null, string? additionalMessage = null) =>
        new(-32600, additionalMessage is null ? "Invalid request" : $"Invalid request: {additionalMessage}", Wrap(data));

    /// <summary>JSON-RPC <c>-32601</c> Method not found.</summary>
    public static RequestErrorException MethodNotFound(string method) =>
        new(-32601, $"\"Method not found\": {method}", Wrap(new { method }));

    /// <summary>JSON-RPC <c>-32602</c> Invalid params.</summary>
    public static RequestErrorException InvalidParams(object? data = null, string? additionalMessage = null) =>
        new(-32602, additionalMessage is null ? "Invalid params" : $"Invalid params: {additionalMessage}", Wrap(data));

    /// <summary>JSON-RPC <c>-32603</c> Internal error.</summary>
    public static RequestErrorException InternalError(object? data = null, string? additionalMessage = null) =>
        new(-32603, additionalMessage is null ? "Internal error" : $"Internal error: {additionalMessage}", Wrap(data));

    /// <summary>ACP <c>-32800</c> Request cancelled (response to <c>$/cancel_request</c>).</summary>
    public static RequestErrorException RequestCancelled(object? data = null) =>
        new(-32800, "Request cancelled", Wrap(data));

    /// <summary>ACP-specific <c>-32000</c> Authentication required.</summary>
    public static RequestErrorException AuthRequired(object? data = null, string? additionalMessage = null) =>
        new(-32000, additionalMessage is null ? "Authentication required" : $"Authentication required: {additionalMessage}", Wrap(data));

    /// <summary>ACP-specific <c>-32002</c> Resource not found.</summary>
    public static RequestErrorException ResourceNotFound(string? uri = null) =>
        new(-32002, uri is null ? "Resource not found" : $"Resource not found: {uri}", uri is null ? null : Wrap(new { uri }));
}
