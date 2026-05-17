using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acp.Schema.Converters;

/// <summary>
/// Polymorphic converter for <see cref="McpServer"/>. Discriminates on the <c>type</c> property;
/// when no <c>type</c> is present we treat the value as a stdio server (the default in older
/// agents) — the JSON Schema only distinguishes stdio by the absence of a <c>type</c> field on
/// the inner allOf.
/// </summary>
public sealed class McpServerJsonConverter : JsonConverter<McpServer>
{
    public override McpServer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement el = DiscriminatedUnionHelper.RequireObject(doc.RootElement, nameof(McpServer));

        string discriminator = "stdio";
        if (el.TryGetProperty("type", out JsonElement typeProp) && typeProp.ValueKind == JsonValueKind.String)
        {
            discriminator = typeProp.GetString()!;
        }

        return discriminator switch
        {
            "stdio" => el.Deserialize<McpServerStdio>(options)!,
            "http" => el.Deserialize<McpServerHttp>(options)!,
            "sse" => el.Deserialize<McpServerSse>(options)!,
            _ => throw new JsonException($"Unknown McpServer type '{discriminator}'."),
        };
    }

    public override void Write(Utf8JsonWriter writer, McpServer value, JsonSerializerOptions options)
    {
        JsonElement obj = JsonSerializer.SerializeToElement(value, value.GetType(), options);
        writer.WriteStartObject();
        if (value.Type is { } t)
        {
            writer.WriteString("type", t);
        }
        foreach (JsonProperty prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, "type", StringComparison.Ordinal)) continue;
            prop.WriteTo(writer);
        }
        writer.WriteEndObject();
    }
}
