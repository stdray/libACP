using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acp.Schema.Converters;

/// <summary>
/// Routes concrete variants of a top-level union (e.g. <see cref="FormElicitationRequest"/>) through
/// the union's converter, so serializing a value by its runtime type (as the JSON-RPC layer does)
/// still produces the discriminated wire shape. The base converter must write every variant
/// manually (it must not re-serialize by runtime type).
/// </summary>
internal sealed class DerivedUnionJsonConverterFactory<TBase>(JsonConverter<TBase> inner) : JsonConverterFactory
    where TBase : class
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert != typeof(TBase) && typeof(TBase).IsAssignableFrom(typeToConvert);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(typeof(Derived<>).MakeGenericType(typeof(TBase), typeToConvert), inner)!;

    private sealed class Derived<TDerived>(JsonConverter<TBase> inner) : JsonConverter<TDerived>
        where TDerived : TBase
    {
        public override TDerived Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            inner.Read(ref reader, typeof(TBase), options) is TDerived d
                ? d
                : throw new JsonException($"JSON does not describe a {typeof(TDerived).Name}.");

        public override void Write(Utf8JsonWriter writer, TDerived value, JsonSerializerOptions options) =>
            inner.Write(writer, value, options);
    }
}
