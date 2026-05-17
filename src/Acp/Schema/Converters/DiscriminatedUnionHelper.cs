using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acp.Schema.Converters;

/// <summary>
/// Helper for writing/reading discriminated unions whose variants are distinguished by a single
/// string-valued property such as <c>"type"</c>, <c>"sessionUpdate"</c>, or <c>"outcome"</c>.
/// </summary>
internal static class DiscriminatedUnionHelper
{
    public static JsonElement RequireObject(JsonElement element, string unionName)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"{unionName} must be a JSON object (got {element.ValueKind}).");
        }
        return element;
    }

    public static string ReadDiscriminator(JsonElement element, string discriminator, string unionName)
    {
        if (!element.TryGetProperty(discriminator, out JsonElement value))
        {
            throw new JsonException($"{unionName} object is missing required discriminator '{discriminator}'.");
        }
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new JsonException($"{unionName}.{discriminator} must be a string (got {value.ValueKind}).");
        }
        return value.GetString()!;
    }
}
