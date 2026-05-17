using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acp.Schema.Converters;

/// <summary>Polymorphic converter for <see cref="ToolCallContent"/>. Discriminated by <c>type</c>.</summary>
public sealed class ToolCallContentJsonConverter : JsonConverter<ToolCallContent>
{
    public override ToolCallContent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement el = DiscriminatedUnionHelper.RequireObject(doc.RootElement, nameof(ToolCallContent));
        string disc = DiscriminatedUnionHelper.ReadDiscriminator(el, "type", nameof(ToolCallContent));
        return disc switch
        {
            "content" => el.Deserialize<ToolCallContentBlock>(options)!,
            "diff" => el.Deserialize<ToolCallContentDiff>(options)!,
            "terminal" => el.Deserialize<ToolCallContentTerminal>(options)!,
            _ => throw new JsonException($"Unknown ToolCallContent type '{disc}'."),
        };
    }

    public override void Write(Utf8JsonWriter writer, ToolCallContent value, JsonSerializerOptions options)
    {
        JsonElement obj = JsonSerializer.SerializeToElement(value, value.GetType(), options);
        writer.WriteStartObject();
        writer.WriteString("type", value.Type);
        foreach (JsonProperty prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, "type", StringComparison.Ordinal)) continue;
            prop.WriteTo(writer);
        }
        writer.WriteEndObject();
    }
}
