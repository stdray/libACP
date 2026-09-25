using System.Text.Json;
using System.Text.Json.Serialization;
using Acp.JsonRpc;

namespace Acp.Schema.Converters;

/// <summary>
/// Converter for <see cref="CreateElicitationRequest"/>. Discriminated by <c>mode</c>; the
/// <see cref="ElicitationScope"/> is flattened into the object (<c>sessionId</c>/<c>toolCallId</c>
/// or <c>requestId</c>). Unknown modes round-trip through <see cref="UnknownElicitationRequest"/>.
/// </summary>
public sealed class CreateElicitationRequestJsonConverter : JsonConverter<CreateElicitationRequest>
{
    public override CreateElicitationRequest Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement el = DiscriminatedUnionHelper.RequireObject(doc.RootElement, nameof(CreateElicitationRequest));
        string mode = DiscriminatedUnionHelper.ReadDiscriminator(el, "mode", nameof(CreateElicitationRequest));

        string message = el.TryGetProperty("message", out JsonElement m) && m.ValueKind == JsonValueKind.String
            ? m.GetString()!
            : throw new JsonException("CreateElicitationRequest is missing required string 'message'.");
        ElicitationScope scope = ReadScope(el, options);
        Meta? meta = ReadOptional<Meta>(el, "_meta", options);

        return mode switch
        {
            "form" => new FormElicitationRequest
            {
                Message = message,
                Scope = scope,
                Meta = meta,
                RequestedSchema = ReadRequired<ElicitationSchema>(el, "requestedSchema", options),
            },
            "url" => new UrlElicitationRequest
            {
                Message = message,
                Scope = scope,
                Meta = meta,
                ElicitationId = ReadRequired<ElicitationId>(el, "elicitationId", options),
                Url = ReadRequired<string>(el, "url", options),
            },
            _ => new UnknownElicitationRequest(mode, el.Clone())
            {
                Message = message,
                Scope = scope,
                Meta = meta,
            },
        };
    }

    public override void Write(Utf8JsonWriter writer, CreateElicitationRequest value, JsonSerializerOptions options)
    {
        if (value is UnknownElicitationRequest unknown)
        {
            unknown.Raw.WriteTo(writer);
            return;
        }

        writer.WriteStartObject();
        writer.WriteString("mode", value.Mode);
        writer.WriteString("message", value.Message);
        WriteScope(writer, value.Scope, options);
        switch (value)
        {
            case FormElicitationRequest form:
                writer.WritePropertyName("requestedSchema");
                JsonSerializer.Serialize(writer, form.RequestedSchema, options);
                break;
            case UrlElicitationRequest url:
                writer.WriteString("elicitationId", url.ElicitationId.Value);
                writer.WriteString("url", url.Url);
                break;
            default:
                throw new JsonException($"Unsupported CreateElicitationRequest variant {value.GetType().Name}.");
        }
        if (value.Meta is not null)
        {
            writer.WritePropertyName("_meta");
            JsonSerializer.Serialize(writer, value.Meta, options);
        }
        writer.WriteEndObject();
    }

    private static ElicitationScope ReadScope(JsonElement el, JsonSerializerOptions options)
    {
        if (el.TryGetProperty("sessionId", out JsonElement sid) && sid.ValueKind != JsonValueKind.Null)
        {
            return new ElicitationSessionScope
            {
                SessionId = sid.Deserialize<SessionId>(options),
                ToolCallId = ReadOptional<ToolCallId?>(el, "toolCallId", options),
            };
        }
        if (el.TryGetProperty("requestId", out JsonElement rid))
        {
            return new ElicitationRequestScope { RequestId = rid.Deserialize<RequestId>(options) };
        }
        throw new JsonException("CreateElicitationRequest must carry either 'sessionId' or 'requestId'.");
    }

    private static void WriteScope(Utf8JsonWriter writer, ElicitationScope scope, JsonSerializerOptions options)
    {
        switch (scope)
        {
            case ElicitationSessionScope s:
                writer.WriteString("sessionId", s.SessionId.Value);
                if (s.ToolCallId is { } tc)
                {
                    writer.WriteString("toolCallId", tc.Value);
                }
                break;
            case ElicitationRequestScope r:
                writer.WritePropertyName("requestId");
                JsonSerializer.Serialize(writer, r.RequestId, options);
                break;
            case null:
                throw new JsonException("CreateElicitationRequest.Scope is required.");
            default:
                throw new JsonException($"Unsupported ElicitationScope variant {scope.GetType().Name}.");
        }
    }

    private static T ReadRequired<T>(JsonElement el, string name, JsonSerializerOptions options)
    {
        if (!el.TryGetProperty(name, out JsonElement v) || v.ValueKind == JsonValueKind.Null)
        {
            throw new JsonException($"CreateElicitationRequest is missing required '{name}'.");
        }
        return v.Deserialize<T>(options)!;
    }

    private static T? ReadOptional<T>(JsonElement el, string name, JsonSerializerOptions options)
        => el.TryGetProperty(name, out JsonElement v) && v.ValueKind != JsonValueKind.Null ? v.Deserialize<T>(options) : default;
}
