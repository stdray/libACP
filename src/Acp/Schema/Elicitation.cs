using System.Text.Json;
using System.Text.Json.Serialization;
using Acp.JsonRpc;
using Acp.Schema.Converters;

namespace Acp.Schema;

// ---------------------------------------------------------------------------------------------
// Capabilities
// ---------------------------------------------------------------------------------------------

/// <summary>
/// Elicitation support advertised by the client in <c>clientCapabilities.elicitation</c>.
/// Each mode is supported only when its field is present and non-null; <c>{}</c> advertises no modes.
/// </summary>
public sealed record ElicitationCapabilities
{
    /// <summary>Present (<c>{}</c>) when the client supports form-mode elicitation.</summary>
    [JsonPropertyName("form")]
    public ElicitationFormCapabilities? Form { get; init; }

    /// <summary>Present (<c>{}</c>) when the client supports URL-mode elicitation.</summary>
    [JsonPropertyName("url")]
    public ElicitationUrlCapabilities? Url { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Marker capability for form-mode elicitation.</summary>
public sealed record ElicitationFormCapabilities
{
    [JsonPropertyName("_meta")] public Meta? Meta { get; init; }
}

/// <summary>Marker capability for URL-mode elicitation.</summary>
public sealed record ElicitationUrlCapabilities
{
    [JsonPropertyName("_meta")] public Meta? Meta { get; init; }
}

// ---------------------------------------------------------------------------------------------
// Ids and scope
// ---------------------------------------------------------------------------------------------

/// <summary>An opaque id of a URL-mode elicitation, chosen by the agent.</summary>
[JsonConverter(typeof(StringIdConverter<ElicitationId>.Json))]
public readonly record struct ElicitationId(string Value)
{
    public override string ToString() => Value;
    public static implicit operator string(ElicitationId id) => id.Value;
}

/// <summary>
/// What an elicitation is bound to. Flattened into the request on the wire: either
/// <c>sessionId</c> (+ optional <c>toolCallId</c>) or <c>requestId</c>.
/// </summary>
public abstract record ElicitationScope
{
    /// <summary>A session-scoped elicitation, optionally tied to a tool call in that session.</summary>
    public static ElicitationSessionScope ForSession(SessionId sessionId, ToolCallId? toolCallId = null)
        => new() { SessionId = sessionId, ToolCallId = toolCallId };

    /// <summary>A request-scoped elicitation outside a session.</summary>
    public static ElicitationRequestScope ForRequest(RequestId requestId)
        => new() { RequestId = requestId };
}

/// <summary>Session scope: <c>sessionId</c> and optional <c>toolCallId</c>.</summary>
public sealed record ElicitationSessionScope : ElicitationScope
{
    [JsonPropertyName("sessionId")]
    public required SessionId SessionId { get; init; }

    [JsonPropertyName("toolCallId")]
    public ToolCallId? ToolCallId { get; init; }
}

/// <summary>Request scope: the JSON-RPC <c>requestId</c> the elicitation belongs to.</summary>
public sealed record ElicitationRequestScope : ElicitationScope
{
    [JsonPropertyName("requestId")]
    public required RequestId RequestId { get; init; }
}

// ---------------------------------------------------------------------------------------------
// elicitation/create request
// ---------------------------------------------------------------------------------------------

/// <summary>
/// Parameters of the <c>elicitation/create</c> request (agent → client). Discriminated by <c>mode</c>:
/// <see cref="FormElicitationRequest"/>, <see cref="UrlElicitationRequest"/>, or
/// <see cref="UnknownElicitationRequest"/> for modes this library does not know.
/// </summary>
[JsonConverter(typeof(CreateElicitationRequestJsonConverter))]
public abstract record CreateElicitationRequest
{
    /// <summary>The wire <c>mode</c> value.</summary>
    [JsonIgnore]
    public abstract string Mode { get; }

    /// <summary>Human-readable explanation of what is requested and why.</summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>What the elicitation is bound to (session or request).</summary>
    [JsonIgnore]
    public required ElicitationScope Scope { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Form mode: collect non-sensitive data described by a restricted JSON Schema.</summary>
public sealed record FormElicitationRequest : CreateElicitationRequest
{
    public override string Mode => "form";

    /// <summary>The flat object schema the user's answer should conform to.</summary>
    [JsonPropertyName("requestedSchema")]
    public required ElicitationSchema RequestedSchema { get; init; }
}

/// <summary>URL mode: open an out-of-band interaction (e.g. OAuth) after user consent.</summary>
public sealed record UrlElicitationRequest : CreateElicitationRequest
{
    public override string Mode => "url";

    /// <summary>Id later echoed by <c>elicitation/complete</c>.</summary>
    [JsonPropertyName("elicitationId")]
    public required ElicitationId ElicitationId { get; init; }

    /// <summary>The URL to open.</summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }
}

/// <summary>An elicitation request with an unrecognized <c>mode</c>. The raw object is kept and written back unchanged.</summary>
public sealed record UnknownElicitationRequest : CreateElicitationRequest
{
    public UnknownElicitationRequest(string mode, JsonElement raw)
    {
        UnknownMode = mode;
        Raw = raw;
    }

    /// <summary>The wire <c>mode</c> value.</summary>
    [JsonIgnore]
    public string UnknownMode { get; }

    /// <summary>The complete params object as received.</summary>
    [JsonIgnore]
    public JsonElement Raw { get; }

    public override string Mode => UnknownMode;
}

// ---------------------------------------------------------------------------------------------
// elicitation/create response
// ---------------------------------------------------------------------------------------------

/// <summary>
/// Result of <c>elicitation/create</c>. Discriminated by <c>action</c>:
/// <see cref="AcceptElicitationResponse"/>, <see cref="DeclineElicitationResponse"/>,
/// <see cref="CancelElicitationResponse"/>, or <see cref="UnknownElicitationResponse"/>.
/// </summary>
[JsonConverter(typeof(CreateElicitationResponseJsonConverter))]
public abstract record CreateElicitationResponse
{
    /// <summary>The wire <c>action</c> value.</summary>
    [JsonIgnore]
    public abstract string Action { get; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>The user submitted the form or consented to open the URL.</summary>
public sealed record AcceptElicitationResponse : CreateElicitationResponse
{
    public override string Action => "accept";

    /// <summary>Submitted form values (normally omitted for URL mode).</summary>
    [JsonPropertyName("content")]
    public IReadOnlyDictionary<string, ElicitationContentValue>? Content { get; init; }
}

/// <summary>The user explicitly declined.</summary>
public sealed record DeclineElicitationResponse : CreateElicitationResponse
{
    public override string Action => "decline";
}

/// <summary>The user dismissed the interaction without choosing.</summary>
public sealed record CancelElicitationResponse : CreateElicitationResponse
{
    public override string Action => "cancel";
}

/// <summary>A response with an unrecognized <c>action</c>. The raw object is kept and written back unchanged.</summary>
public sealed record UnknownElicitationResponse : CreateElicitationResponse
{
    public UnknownElicitationResponse(string action, JsonElement raw)
    {
        UnknownAction = action;
        Raw = raw;
    }

    /// <summary>The wire <c>action</c> value.</summary>
    [JsonIgnore]
    public string UnknownAction { get; }

    /// <summary>The complete result object as received.</summary>
    [JsonIgnore]
    public JsonElement Raw { get; }

    public override string Action => UnknownAction;
}

/// <summary>The kind of a submitted elicitation value.</summary>
public enum ElicitationContentValueKind
{
    String,
    Integer,
    Number,
    Boolean,
    StringArray,
}

/// <summary>
/// A submitted form value: a string, integer, number, boolean, or array of strings.
/// Implicit conversions exist from the corresponding .NET types.
/// </summary>
[JsonConverter(typeof(ElicitationContentValueJsonConverter))]
public sealed class ElicitationContentValue : IEquatable<ElicitationContentValue>
{
    private readonly object _value;

    private ElicitationContentValue(ElicitationContentValueKind kind, object value)
    {
        Kind = kind;
        _value = value;
    }

    public ElicitationContentValueKind Kind { get; }

    public static ElicitationContentValue FromString(string value) => new(ElicitationContentValueKind.String, value ?? throw new ArgumentNullException(nameof(value)));
    public static ElicitationContentValue FromInteger(long value) => new(ElicitationContentValueKind.Integer, value);
    public static ElicitationContentValue FromNumber(double value) => new(ElicitationContentValueKind.Number, value);
    public static ElicitationContentValue FromBoolean(bool value) => new(ElicitationContentValueKind.Boolean, value);
    public static ElicitationContentValue FromStringArray(IReadOnlyList<string> value) => new(ElicitationContentValueKind.StringArray, value ?? throw new ArgumentNullException(nameof(value)));

    public static implicit operator ElicitationContentValue(string value) => FromString(value);
    public static implicit operator ElicitationContentValue(long value) => FromInteger(value);
    public static implicit operator ElicitationContentValue(int value) => FromInteger(value);
    public static implicit operator ElicitationContentValue(double value) => FromNumber(value);
    public static implicit operator ElicitationContentValue(bool value) => FromBoolean(value);
    public static implicit operator ElicitationContentValue(string[] value) => FromStringArray(value);

    public string AsString() => Kind == ElicitationContentValueKind.String ? (string)_value : throw WrongKind(ElicitationContentValueKind.String);
    public long AsInteger() => Kind == ElicitationContentValueKind.Integer ? (long)_value : throw WrongKind(ElicitationContentValueKind.Integer);

    /// <summary>The numeric value; also accepts <see cref="ElicitationContentValueKind.Integer"/> values.</summary>
    public double AsNumber() => Kind switch
    {
        ElicitationContentValueKind.Number => (double)_value,
        ElicitationContentValueKind.Integer => (long)_value,
        _ => throw WrongKind(ElicitationContentValueKind.Number),
    };

    public bool AsBoolean() => Kind == ElicitationContentValueKind.Boolean ? (bool)_value : throw WrongKind(ElicitationContentValueKind.Boolean);
    public IReadOnlyList<string> AsStringArray() => Kind == ElicitationContentValueKind.StringArray ? (IReadOnlyList<string>)_value : throw WrongKind(ElicitationContentValueKind.StringArray);

    private InvalidOperationException WrongKind(ElicitationContentValueKind expected) =>
        new($"ElicitationContentValue is {Kind}, not {expected}.");

    public bool Equals(ElicitationContentValue? other) =>
        other is not null && Kind == other.Kind && (Kind == ElicitationContentValueKind.StringArray
            ? AsStringArray().SequenceEqual(other.AsStringArray())
            : _value.Equals(other._value));

    public override bool Equals(object? obj) => obj is ElicitationContentValue v && Equals(v);

    public override int GetHashCode() => Kind == ElicitationContentValueKind.StringArray
        ? HashCode.Combine(Kind, AsStringArray().Count)
        : HashCode.Combine(Kind, _value);

    public override string ToString() => Kind == ElicitationContentValueKind.StringArray
        ? "[" + string.Join(", ", AsStringArray()) + "]"
        : Convert.ToString(_value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
}

// ---------------------------------------------------------------------------------------------
// elicitation/complete notification
// ---------------------------------------------------------------------------------------------

/// <summary>Parameters of the <c>elicitation/complete</c> notification: a URL-mode interaction finished.</summary>
public sealed record CompleteElicitationNotification
{
    [JsonPropertyName("elicitationId")]
    public required ElicitationId ElicitationId { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

// ---------------------------------------------------------------------------------------------
// Requested schema
// ---------------------------------------------------------------------------------------------

/// <summary>A flat object schema describing the form fields of a form-mode elicitation.</summary>
public sealed record ElicitationSchema
{
    /// <summary>Always <c>"object"</c>.</summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "object";

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>Form fields keyed by property name.</summary>
    [JsonPropertyName("properties")]
    public IReadOnlyDictionary<string, ElicitationPropertySchema> Properties { get; init; } = new Dictionary<string, ElicitationPropertySchema>();

    /// <summary>Names of required properties.</summary>
    [JsonPropertyName("required")]
    public IReadOnlyList<string>? Required { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Well-known values of <see cref="StringPropertySchema.Format"/>.</summary>
public static class ElicitationStringFormat
{
    public const string Email = "email";
    public const string Uri = "uri";
    public const string Date = "date";
    public const string DateTime = "date-time";
}

/// <summary>
/// A single form field schema. Discriminated by <c>type</c>: <c>string</c>, <c>number</c>,
/// <c>integer</c>, <c>boolean</c>, <c>array</c> (multi-select), or unknown.
/// </summary>
[JsonConverter(typeof(ElicitationPropertySchemaJsonConverter))]
public abstract record ElicitationPropertySchema
{
    /// <summary>The wire <c>type</c> value.</summary>
    [JsonIgnore]
    public abstract string SchemaType { get; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>A string field, optionally a single-select enum (<c>enum</c> or titled <c>oneOf</c>).</summary>
public sealed record StringPropertySchema : ElicitationPropertySchema
{
    public override string SchemaType => "string";

    [JsonPropertyName("minLength")]
    public uint? MinLength { get; init; }

    [JsonPropertyName("maxLength")]
    public uint? MaxLength { get; init; }

    [JsonPropertyName("pattern")]
    public string? Pattern { get; init; }

    /// <summary>See <see cref="ElicitationStringFormat"/>.</summary>
    [JsonPropertyName("format")]
    public string? Format { get; init; }

    [JsonPropertyName("default")]
    public string? Default { get; init; }

    /// <summary>Allowed values (untitled single-select).</summary>
    [JsonPropertyName("enum")]
    public IReadOnlyList<string>? Enum { get; init; }

    /// <summary>Allowed values with display titles (titled single-select).</summary>
    [JsonPropertyName("oneOf")]
    public IReadOnlyList<EnumOption>? OneOf { get; init; }
}

/// <summary>A floating-point number field.</summary>
public sealed record NumberPropertySchema : ElicitationPropertySchema
{
    public override string SchemaType => "number";

    [JsonPropertyName("minimum")]
    public double? Minimum { get; init; }

    [JsonPropertyName("maximum")]
    public double? Maximum { get; init; }

    [JsonPropertyName("default")]
    public double? Default { get; init; }
}

/// <summary>An integer field.</summary>
public sealed record IntegerPropertySchema : ElicitationPropertySchema
{
    public override string SchemaType => "integer";

    [JsonPropertyName("minimum")]
    public long? Minimum { get; init; }

    [JsonPropertyName("maximum")]
    public long? Maximum { get; init; }

    [JsonPropertyName("default")]
    public long? Default { get; init; }
}

/// <summary>A boolean field.</summary>
public sealed record BooleanPropertySchema : ElicitationPropertySchema
{
    public override string SchemaType => "boolean";

    [JsonPropertyName("default")]
    public bool? Default { get; init; }
}

/// <summary>A multi-select field (<c>type: "array"</c>) whose items are string options.</summary>
public sealed record MultiSelectPropertySchema : ElicitationPropertySchema
{
    public override string SchemaType => "array";

    [JsonPropertyName("minItems")]
    public ulong? MinItems { get; init; }

    [JsonPropertyName("maxItems")]
    public ulong? MaxItems { get; init; }

    [JsonPropertyName("items")]
    public required MultiSelectItems Items { get; init; }

    [JsonPropertyName("default")]
    public IReadOnlyList<string>? Default { get; init; }
}

/// <summary>A property schema with an unrecognized <c>type</c>. The raw object is kept and written back unchanged.</summary>
public sealed record UnknownPropertySchema : ElicitationPropertySchema
{
    public UnknownPropertySchema(string type, JsonElement raw)
    {
        UnknownType = type;
        Raw = raw;
    }

    [JsonIgnore]
    public string UnknownType { get; }

    [JsonIgnore]
    public JsonElement Raw { get; }

    public override string SchemaType => UnknownType;
}

/// <summary>A selectable option with a display title.</summary>
public sealed record EnumOption
{
    /// <summary>The value submitted when this option is chosen.</summary>
    [JsonPropertyName("const")]
    public required string Const { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>
/// Item schema of a multi-select field: <see cref="StringMultiSelectItems"/> (<c>type: "string"</c> + <c>enum</c>),
/// <see cref="TitledMultiSelectItems"/> (<c>anyOf</c> of <see cref="EnumOption"/>), or unknown.
/// </summary>
[JsonConverter(typeof(MultiSelectItemsJsonConverter))]
public abstract record MultiSelectItems
{
    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Untitled options: <c>{"type":"string","enum":[...]}</c>.</summary>
public sealed record StringMultiSelectItems : MultiSelectItems
{
    [JsonPropertyName("enum")]
    public required IReadOnlyList<string> Enum { get; init; }
}

/// <summary>Titled options: <c>{"anyOf":[{"const":...,"title":...}]}</c>.</summary>
public sealed record TitledMultiSelectItems : MultiSelectItems
{
    [JsonPropertyName("anyOf")]
    public required IReadOnlyList<EnumOption> AnyOf { get; init; }
}

/// <summary>An unrecognized multi-select item schema. The raw object is kept and written back unchanged.</summary>
public sealed record UnknownMultiSelectItems : MultiSelectItems
{
    public UnknownMultiSelectItems(JsonElement raw) => Raw = raw;

    [JsonIgnore]
    public JsonElement Raw { get; }
}
