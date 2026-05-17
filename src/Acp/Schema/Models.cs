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
/// A configuration option that the agent exposes for per-session tuning (e.g. temperature, tools).
/// The exact structure is intentionally permissive to accommodate the variety of option categories
/// in the protocol.
/// </summary>
public sealed record ConfigOption
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("currentValue")]
    public JsonElement? CurrentValue { get; init; }

    [JsonPropertyName("defaultValue")]
    public JsonElement? DefaultValue { get; init; }

    [JsonPropertyName("options")]
    public IReadOnlyList<ConfigOptionValue>? Options { get; init; }

    [JsonPropertyName("possibleValues")]
    public IReadOnlyList<string>? PossibleValues { get; init; }

    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>A named value for a configuration option.</summary>
public sealed record ConfigOptionValue
{
    [JsonPropertyName("value")]
    public string? Value { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
