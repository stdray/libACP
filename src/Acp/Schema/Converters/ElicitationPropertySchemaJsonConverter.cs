using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acp.Schema.Converters;

/// <summary>
/// Polymorphic converter for <see cref="ElicitationPropertySchema"/>. Discriminated by <c>type</c>;
/// unknown types round-trip through <see cref="UnknownPropertySchema"/>.
/// </summary>
public sealed class ElicitationPropertySchemaJsonConverter : JsonConverter<ElicitationPropertySchema>
{
    public override ElicitationPropertySchema Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement el = DiscriminatedUnionHelper.RequireObject(doc.RootElement, nameof(ElicitationPropertySchema));
        string disc = DiscriminatedUnionHelper.ReadDiscriminator(el, "type", nameof(ElicitationPropertySchema));
        return disc switch
        {
            "string" => el.Deserialize<StringPropertySchema>(options)!,
            "number" => el.Deserialize<NumberPropertySchema>(options)!,
            "integer" => el.Deserialize<IntegerPropertySchema>(options)!,
            "boolean" => el.Deserialize<BooleanPropertySchema>(options)!,
            "array" => el.Deserialize<MultiSelectPropertySchema>(options)!,
            _ => new UnknownPropertySchema(disc, el.Clone()),
        };
    }

    public override void Write(Utf8JsonWriter writer, ElicitationPropertySchema value, JsonSerializerOptions options)
    {
        if (value is UnknownPropertySchema unknown)
        {
            unknown.Raw.WriteTo(writer);
            return;
        }

        JsonElement obj = JsonSerializer.SerializeToElement(value, value.GetType(), options);
        writer.WriteStartObject();
        writer.WriteString("type", value.SchemaType);
        foreach (JsonProperty prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, "type", StringComparison.Ordinal)) continue;
            if (string.Equals(prop.Name, "schemaType", StringComparison.Ordinal)) continue;
            prop.WriteTo(writer);
        }
        writer.WriteEndObject();
    }
}
