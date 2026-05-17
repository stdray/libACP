using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acp.Schema.Converters;

/// <summary>
/// Converter for <see cref="EmbeddedResourceResource"/>. The variants are not discriminated by a
/// dedicated property — they are distinguished by which payload field is present (<c>text</c> or
/// <c>blob</c>).
/// </summary>
public sealed class EmbeddedResourceResourceJsonConverter : JsonConverter<EmbeddedResourceResource>
{
    public override EmbeddedResourceResource Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement el = DiscriminatedUnionHelper.RequireObject(doc.RootElement, nameof(EmbeddedResourceResource));

        bool hasText = el.TryGetProperty("text", out _);
        bool hasBlob = el.TryGetProperty("blob", out _);
        if (hasText && !hasBlob) return el.Deserialize<TextResourceContents>(options)!;
        if (hasBlob && !hasText) return el.Deserialize<BlobResourceContents>(options)!;
        throw new JsonException(
            "EmbeddedResourceResource must contain exactly one of 'text' or 'blob'.");
    }

    public override void Write(Utf8JsonWriter writer, EmbeddedResourceResource value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
