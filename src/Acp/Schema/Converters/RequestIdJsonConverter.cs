using System.Text.Json;
using System.Text.Json.Serialization;
using Acp.JsonRpc;

namespace Acp.Schema.Converters;

/// <summary>
/// JSON converter for <see cref="RequestId"/>. Reads any of <c>string</c>, integer JSON number, or
/// <c>null</c>; writes back exactly the wire shape that was supplied. Notification-style "no id"
/// values are written by simply omitting the property at the higher level (this converter throws
/// when asked to write the <see cref="RequestId.None"/> sentinel).
/// </summary>
public sealed class RequestIdJsonConverter : JsonConverter<RequestId>
{
    public override RequestId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.Number when reader.TryGetInt64(out long n) => RequestId.FromNumber(n),
            JsonTokenType.String => RequestId.FromString(reader.GetString()!),
            JsonTokenType.Null => RequestId.Null(),
            _ => throw new JsonException($"Invalid JSON-RPC id token: {reader.TokenType}"),
        };

    public override void Write(Utf8JsonWriter writer, RequestId value, JsonSerializerOptions options)
    {
        switch (value.Kind)
        {
            case RequestIdKind.Number:
                writer.WriteNumberValue(value.AsNumber());
                break;
            case RequestIdKind.String:
                writer.WriteStringValue(value.AsString());
                break;
            case RequestIdKind.Null:
                writer.WriteNullValue();
                break;
            case RequestIdKind.None:
            default:
                throw new InvalidOperationException(
                    "Cannot serialize RequestId.None — notifications must omit the 'id' property entirely.");
        }
    }
}
