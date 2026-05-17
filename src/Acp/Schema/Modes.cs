using System.Text.Json.Serialization;

namespace Acp.Schema;

/// <summary>One operating mode the agent can run in (e.g. ask, code, architect).</summary>
public sealed record SessionMode
{
    [JsonPropertyName("id")]
    public required SessionModeId Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Mode state advertised by the agent for a session.</summary>
public sealed record SessionModeState
{
    [JsonPropertyName("currentModeId")]
    public required SessionModeId CurrentModeId { get; init; }

    [JsonPropertyName("availableModes")]
    public required IReadOnlyList<SessionMode> AvailableModes { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Parameters for the <c>session/set_mode</c> method.</summary>
public sealed record SetSessionModeRequest
{
    [JsonPropertyName("sessionId")]
    public required SessionId SessionId { get; init; }

    [JsonPropertyName("modeId")]
    public required SessionModeId ModeId { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

public sealed record SetSessionModeResponse
{
    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Parameters for the <c>session/set_model</c> method.</summary>
public sealed record SetSessionModelRequest
{
    [JsonPropertyName("sessionId")]
    public required SessionId SessionId { get; init; }

    [JsonPropertyName("modelId")]
    public required string ModelId { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

public sealed record SetSessionModelResponse
{
    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Parameters for the <c>session/set_config_option</c> method.</summary>
public sealed record SetSessionConfigOptionRequest
{
    [JsonPropertyName("sessionId")]
    public required SessionId SessionId { get; init; }

    [JsonPropertyName("optionId")]
    public required string OptionId { get; init; }

    [JsonPropertyName("valueId")]
    public string? ValueId { get; init; }

    [JsonPropertyName("value")]
    public System.Text.Json.JsonElement? Value { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>
/// Response to <c>session/set_config_option</c>. The agent returns the full updated set of
/// options. The exact structure of an option is intentionally permissive (passed through as a
/// <see cref="System.Text.Json.JsonElement"/>) to accommodate the variety of option categories
/// defined in the protocol without forcing a hard dependency on every variant.
/// </summary>
public sealed record SetSessionConfigOptionResponse
{
    [JsonPropertyName("options")]
    public IReadOnlyList<System.Text.Json.JsonElement>? Options { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}
