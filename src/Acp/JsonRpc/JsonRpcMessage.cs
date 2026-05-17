using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Acp.JsonRpc;

/// <summary>
/// JSON-RPC 2.0 request id.
///
/// Represents one of: a string, a 64-bit signed integer, or null. Notifications omit the id entirely
/// (use <see cref="None"/> as a sentinel and serialization is suppressed by the converter).
/// </summary>
[JsonConverter(typeof(Acp.Schema.Converters.RequestIdJsonConverter))]
public readonly struct RequestId : IEquatable<RequestId>
{
    /// <summary>Sentinel value indicating "no id" (notifications). Not a valid wire value.</summary>
    public static readonly RequestId None = default;

    private readonly string? _string;
    private readonly long _number;
    private readonly RequestIdKind _kind;

    private RequestId(RequestIdKind kind, long number, string? str)
    {
        _kind = kind;
        _number = number;
        _string = str;
    }

    /// <summary>The kind of id this value carries.</summary>
    public RequestIdKind Kind => _kind;

    /// <summary>Returns true when this value represents "no id" (i.e. a notification).</summary>
    public bool IsNone => _kind == RequestIdKind.None;

    /// <summary>Create a numeric request id.</summary>
    public static RequestId FromNumber(long value) => new(RequestIdKind.Number, value, null);

    /// <summary>Create a string request id.</summary>
    public static RequestId FromString(string value) =>
        new(RequestIdKind.String, 0, value ?? throw new ArgumentNullException(nameof(value)));

    /// <summary>Create a JSON-RPC null id (legal but discouraged by the spec).</summary>
    public static RequestId Null() => new(RequestIdKind.Null, 0, null);

    /// <summary>Get the numeric value if <see cref="Kind"/> is <see cref="RequestIdKind.Number"/>.</summary>
    public long AsNumber() => _kind == RequestIdKind.Number
        ? _number
        : throw new InvalidOperationException($"RequestId is not Number (kind={_kind}).");

    /// <summary>Get the string value if <see cref="Kind"/> is <see cref="RequestIdKind.String"/>.</summary>
    public string AsString() => _kind == RequestIdKind.String
        ? _string!
        : throw new InvalidOperationException($"RequestId is not String (kind={_kind}).");

    public bool Equals(RequestId other) =>
        _kind == other._kind &&
        _kind switch
        {
            RequestIdKind.Number => _number == other._number,
            RequestIdKind.String => string.Equals(_string, other._string, StringComparison.Ordinal),
            _ => true,
        };

    public override bool Equals(object? obj) => obj is RequestId other && Equals(other);

    public override int GetHashCode() => _kind switch
    {
        RequestIdKind.Number => HashCode.Combine(_kind, _number),
        RequestIdKind.String => HashCode.Combine(_kind, _string),
        _ => HashCode.Combine(_kind),
    };

    public override string ToString() => _kind switch
    {
        RequestIdKind.Number => _number.ToString(System.Globalization.CultureInfo.InvariantCulture),
        RequestIdKind.String => _string!,
        RequestIdKind.Null => "null",
        _ => "<none>",
    };

    public static bool operator ==(RequestId left, RequestId right) => left.Equals(right);
    public static bool operator !=(RequestId left, RequestId right) => !left.Equals(right);
}

/// <summary>The variant of a <see cref="RequestId"/>.</summary>
public enum RequestIdKind
{
    /// <summary>The id is absent (notification or sentinel).</summary>
    None = 0,
    /// <summary>The id is a JSON number.</summary>
    Number,
    /// <summary>The id is a JSON string.</summary>
    String,
    /// <summary>The id is JSON null.</summary>
    Null,
}

/// <summary>
/// A decoded JSON-RPC 2.0 message envelope. Exactly one of request, response, or notification will
/// be populated based on the wire shape.
/// </summary>
public sealed class JsonRpcMessage
{
    /// <summary>Always "2.0".</summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    /// <summary>Method name (present on requests and notifications).</summary>
    [JsonPropertyName("method")]
    public string? Method { get; init; }

    /// <summary>Request/response id. Absent on notifications.</summary>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public RequestId Id { get; init; }

    /// <summary>Whether <see cref="Id"/> was present in the original message (used to distinguish notifications).</summary>
    [JsonIgnore]
    public bool HasId { get; init; }

    /// <summary>Method parameters (request/notification).</summary>
    [JsonPropertyName("params")]
    public JsonElement? Params { get; init; }

    /// <summary>Successful response result.</summary>
    [JsonPropertyName("result")]
    public JsonElement? Result { get; init; }

    /// <summary>Error response.</summary>
    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; init; }

    public bool IsRequest => Method is not null && HasId;
    public bool IsNotification => Method is not null && !HasId;
    public bool IsResponse => Method is null && HasId;
}

/// <summary>JSON-RPC 2.0 error object.</summary>
public sealed class JsonRpcError
{
    [JsonPropertyName("code")]
    public required int Code { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("data")]
    public JsonElement? Data { get; init; }
}
