using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acp.Schema.Converters;

/// <summary>
/// Converter for <see cref="CreateElicitationResponse"/>. Discriminated by <c>action</c>; unknown
/// actions round-trip through <see cref="UnknownElicitationResponse"/>.
/// </summary>
public sealed class CreateElicitationResponseJsonConverter : JsonConverter<CreateElicitationResponse>
{
    public override CreateElicitationResponse Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement el = DiscriminatedUnionHelper.RequireObject(doc.RootElement, nameof(CreateElicitationResponse));
        string action = DiscriminatedUnionHelper.ReadDiscriminator(el, "action", nameof(CreateElicitationResponse));
        Meta? meta = el.TryGetProperty("_meta", out JsonElement m) && m.ValueKind == JsonValueKind.Object
            ? m.Deserialize<Meta>(options)
            : null;

        return action switch
        {
            "accept" => new AcceptElicitationResponse
            {
                Meta = meta,
                Content = el.TryGetProperty("content", out JsonElement c) && c.ValueKind != JsonValueKind.Null
                    ? c.Deserialize<Dictionary<string, ElicitationContentValue>>(options)
                    : null,
            },
            "decline" => new DeclineElicitationResponse { Meta = meta },
            "cancel" => new CancelElicitationResponse { Meta = meta },
            _ => new UnknownElicitationResponse(action, el.Clone()) { Meta = meta },
        };
    }

    public override void Write(Utf8JsonWriter writer, CreateElicitationResponse value, JsonSerializerOptions options)
    {
        if (value is UnknownElicitationResponse unknown)
        {
            unknown.Raw.WriteTo(writer);
            return;
        }

        writer.WriteStartObject();
        writer.WriteString("action", value.Action);
        if (value is AcceptElicitationResponse { Content: { } content })
        {
            writer.WritePropertyName("content");
            JsonSerializer.Serialize(writer, content, options);
        }
        if (value.Meta is not null)
        {
            writer.WritePropertyName("_meta");
            JsonSerializer.Serialize(writer, value.Meta, options);
        }
        writer.WriteEndObject();
    }
}
