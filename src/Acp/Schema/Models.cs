using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acp.Schema;

/// <summary>A model available for use in a session, as advertised by the agent.</summary>
public sealed record AgentModel
{
    /// <summary>Primary model identifier (used by most agents).</summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>Alternative model identifier (Copilot uses <c>modelId</c> instead of <c>id</c>).</summary>
    [JsonPropertyName("modelId")]
    public string? ModelId { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }

    /// <summary>Returns whichever ID field is populated (prefers <c>modelId</c> over <c>id</c>).</summary>
    [JsonIgnore]
    public string EffectiveId => ModelId ?? Id ?? Name ?? "";
}

/// <summary>
/// Container for the nested <c>"models"</c> object that some agents (Copilot) return in
/// session/new and session/load responses.
/// </summary>
public sealed record AgentModelState
{
    [JsonPropertyName("availableModels")]
    public IReadOnlyList<AgentModel>? AvailableModels { get; init; }

    [JsonPropertyName("currentModelId")]
    public string? CurrentModelId { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>
/// A session configuration option exposed by the agent (<c>SessionConfigOption</c> in the schema).
/// <see cref="Type"/> is <c>"select"</c> (<see cref="CurrentValue"/> is a value id string and
/// <see cref="Options"/> lists the choices) or <c>"boolean"</c> (<see cref="CurrentValue"/> is a bool).
/// </summary>
public sealed record ConfigOption
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary><c>"select"</c> or <c>"boolean"</c>.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>Semantic category: <c>mode</c>, <c>model</c>, <c>model_config</c>, <c>thought_level</c>, or a custom string.</summary>
    [JsonPropertyName("category")]
    public string? Category { get; init; }

    /// <summary>A value id string for <c>select</c> options, a bool for <c>boolean</c> options.</summary>
    [JsonPropertyName("currentValue")]
    public JsonElement? CurrentValue { get; init; }

    /// <summary>
    /// Choices of a <c>select</c> option. Either a flat list of values, or a list of groups
    /// (entries with <see cref="ConfigOptionValue.Group"/> and nested <see cref="ConfigOptionValue.Options"/>).
    /// </summary>
    [JsonPropertyName("options")]
    public IReadOnlyList<ConfigOptionValue>? Options { get; init; }

    /// <remarks>Not part of the ACP schema.</remarks>
    [JsonPropertyName("defaultValue")]
    public JsonElement? DefaultValue { get; init; }

    /// <remarks>Not part of the ACP schema.</remarks>
    [JsonPropertyName("possibleValues")]
    public IReadOnlyList<string>? PossibleValues { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }

    /// <summary>All selectable values, flattening groups.</summary>
    [JsonIgnore]
    public IEnumerable<ConfigOptionValue> AllValues =>
        (Options ?? []).SelectMany(o => o.Group is not null ? o.Options ?? [] : [o]);
}

/// <summary>
/// A choice of a <c>select</c> config option (<c>SessionConfigSelectOption</c>), or a group of
/// choices (<c>SessionConfigSelectGroup</c>) when <see cref="Group"/> is set.
/// </summary>
public sealed record ConfigOptionValue
{
    [JsonPropertyName("value")]
    public string? Value { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>Group id; set only on group entries.</summary>
    [JsonPropertyName("group")]
    public string? Group { get; init; }

    /// <summary>Choices inside a group; set only on group entries.</summary>
    [JsonPropertyName("options")]
    public IReadOnlyList<ConfigOptionValue>? Options { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}
