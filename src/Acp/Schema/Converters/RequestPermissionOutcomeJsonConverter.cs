using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acp.Schema.Converters;

/// <summary>Polymorphic converter for <see cref="RequestPermissionOutcome"/>. Discriminated by <c>outcome</c>.</summary>
public sealed class RequestPermissionOutcomeJsonConverter : JsonConverter<RequestPermissionOutcome>
{
    public override RequestPermissionOutcome Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement el = DiscriminatedUnionHelper.RequireObject(doc.RootElement, nameof(RequestPermissionOutcome));
        string disc = DiscriminatedUnionHelper.ReadDiscriminator(el, "outcome", nameof(RequestPermissionOutcome));
        return disc switch
        {
            "cancelled" => new CancelledPermissionOutcome(),
            "selected" => el.Deserialize<SelectedPermissionOutcome>(options)!,
            _ => throw new JsonException($"Unknown RequestPermissionOutcome '{disc}'."),
        };
    }

    public override void Write(Utf8JsonWriter writer, RequestPermissionOutcome value, JsonSerializerOptions options)
    {
        JsonElement obj = JsonSerializer.SerializeToElement(value, value.GetType(), options);
        writer.WriteStartObject();
        writer.WriteString("outcome", value.Outcome);
        foreach (JsonProperty prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, "outcome", StringComparison.Ordinal)) continue;
            prop.WriteTo(writer);
        }
        writer.WriteEndObject();
    }
}
