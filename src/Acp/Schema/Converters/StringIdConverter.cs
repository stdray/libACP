using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acp.Schema.Converters;

/// <summary>
/// Generic converter factory for typed string-id wrappers (e.g. <see cref="SessionId"/>). The id
/// type must be a record struct with a single <c>Value</c> property of type <see cref="string"/>.
/// </summary>
internal static class StringIdConverter<T> where T : struct
{
    public sealed class Json : JsonConverter<T>
    {
        private static readonly Func<string, T> Construct = CreateConstructor();

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException($"Expected string for {typeof(T).Name}, got {reader.TokenType}.");
            return Construct(reader.GetString()!);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            string s = (string)typeof(T).GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)!
                .GetValue(value)!;
            writer.WriteStringValue(s);
        }

        private static Func<string, T> CreateConstructor()
        {
            ConstructorInfo? ctor = typeof(T).GetConstructor(new[] { typeof(string) })
                ?? throw new InvalidOperationException($"{typeof(T).Name} must have a (string) constructor.");
            return s => (T)ctor.Invoke(new object[] { s });
        }
    }
}
