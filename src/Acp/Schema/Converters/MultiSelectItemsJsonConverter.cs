using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acp.Schema.Converters;

/// <summary>
/// Converter for the untagged <see cref="MultiSelectItems"/> union: <c>type: "string"</c> selects
/// <see cref="StringMultiSelectItems"/>; no <c>type</c> with <c>anyOf</c> selects
/// <see cref="TitledMultiSelectItems"/>; anything else is kept as <see cref="UnknownMultiSelectItems"/>.
/// </summary>
public sealed class MultiSelectItemsJsonConverter : JsonConverter<MultiSelectItems>
{
    public override MultiSelectItems Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement el = DiscriminatedUnionHelper.RequireObject(doc.RootElement, nameof(MultiSelectItems));
        if (el.TryGetProperty("type", out JsonElement type))
        {
            return type.ValueKind == JsonValueKind.String && type.GetString() == "string"
                ? el.Deserialize<StringMultiSelectItems>(options)!
                : new UnknownMultiSelectItems(el.Clone());
        }
        return el.TryGetProperty("anyOf", out _)
            ? el.Deserialize<TitledMultiSelectItems>(options)!
            : new UnknownMultiSelectItems(el.Clone());
    }

    public override void Write(Utf8JsonWriter writer, MultiSelectItems value, JsonSerializerOptions options)
    {
        if (value is UnknownMultiSelectItems unknown)
        {
            unknown.Raw.WriteTo(writer);
            return;
        }

        JsonElement obj = JsonSerializer.SerializeToElement(value, value.GetType(), options);
        writer.WriteStartObject();
        if (value is StringMultiSelectItems)
        {
            writer.WriteString("type", "string");
        }
        foreach (JsonProperty prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, "type", StringComparison.Ordinal)) continue;
            prop.WriteTo(writer);
        }
        writer.WriteEndObject();
    }
}
