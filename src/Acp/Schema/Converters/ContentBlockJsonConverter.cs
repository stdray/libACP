using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acp.Schema.Converters;

/// <summary>
/// Polymorphic converter for <see cref="ContentBlock"/>. Dispatches on the <c>type</c> property.
/// </summary>
public sealed class ContentBlockJsonConverter : JsonConverter<ContentBlock>
{
    public override ContentBlock Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement el = DiscriminatedUnionHelper.RequireObject(doc.RootElement, nameof(ContentBlock));
        string disc = DiscriminatedUnionHelper.ReadDiscriminator(el, "type", nameof(ContentBlock));
        return disc switch
        {
            "text" => el.Deserialize<TextContent>(options)!,
            "image" => el.Deserialize<ImageContent>(options)!,
            "audio" => el.Deserialize<AudioContent>(options)!,
            "resource_link" => el.Deserialize<ResourceLinkContent>(options)!,
            "resource" => el.Deserialize<EmbeddedResourceContent>(options)!,
            _ => throw new JsonException($"Unknown ContentBlock type '{disc}'."),
        };
    }

    public override void Write(Utf8JsonWriter writer, ContentBlock value, JsonSerializerOptions options)
    {
        // Serialize the concrete record, then inject the "type" discriminator.
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
