using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acp.Schema.Converters;

/// <summary>
/// Converter for the untagged <see cref="ElicitationContentValue"/> union
/// (string | integer | number | boolean | string[]). Integral JSON numbers read as
/// <see cref="ElicitationContentValueKind.Integer"/>, others as <see cref="ElicitationContentValueKind.Number"/>.
/// </summary>
public sealed class ElicitationContentValueJsonConverter : JsonConverter<ElicitationContentValue>
{
    public override ElicitationContentValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return ElicitationContentValue.FromString(reader.GetString()!);
            case JsonTokenType.Number:
                return reader.TryGetInt64(out long l)
                    ? ElicitationContentValue.FromInteger(l)
                    : ElicitationContentValue.FromNumber(reader.GetDouble());
            case JsonTokenType.True:
            case JsonTokenType.False:
                return ElicitationContentValue.FromBoolean(reader.GetBoolean());
            case JsonTokenType.StartArray:
                var items = new List<string>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType != JsonTokenType.String)
                    {
                        throw new JsonException($"Elicitation content arrays must contain strings (got {reader.TokenType}).");
                    }
                    items.Add(reader.GetString()!);
                }
                return ElicitationContentValue.FromStringArray(items);
            default:
                throw new JsonException($"Invalid elicitation content value token: {reader.TokenType}.");
        }
    }

    public override void Write(Utf8JsonWriter writer, ElicitationContentValue value, JsonSerializerOptions options)
    {
        switch (value.Kind)
        {
            case ElicitationContentValueKind.String:
                writer.WriteStringValue(value.AsString());
                break;
            case ElicitationContentValueKind.Integer:
                writer.WriteNumberValue(value.AsInteger());
                break;
            case ElicitationContentValueKind.Number:
                writer.WriteNumberValue(value.AsNumber());
                break;
            case ElicitationContentValueKind.Boolean:
                writer.WriteBooleanValue(value.AsBoolean());
                break;
            case ElicitationContentValueKind.StringArray:
                writer.WriteStartArray();
                foreach (string s in value.AsStringArray())
                {
                    writer.WriteStringValue(s);
                }
                writer.WriteEndArray();
                break;
            default:
                throw new JsonException($"Unsupported ElicitationContentValue kind {value.Kind}.");
        }
    }
}
