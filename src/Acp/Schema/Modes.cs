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
/// <remarks>
/// For a <c>select</c> option <see cref="Value"/> is the chosen value id; for a <c>boolean</c>
/// option it is a bool and <see cref="Type"/> must be <c>"boolean"</c>. Prefer the
/// <see cref="Select"/> / <see cref="Boolean"/> factories.
/// </remarks>
public sealed record SetSessionConfigOptionRequest
{
    [JsonPropertyName("sessionId")]
    public required SessionId SessionId { get; init; }

    [JsonPropertyName("configId")]
    public required string ConfigId { get; init; }

    [JsonPropertyName("value")]
    public required System.Text.Json.JsonElement Value { get; init; }

    /// <summary><c>"boolean"</c> for boolean options; omitted for select options.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }

    /// <summary>Selects <paramref name="valueId"/> for a <c>select</c> option.</summary>
    public static SetSessionConfigOptionRequest Select(SessionId sessionId, string configId, string valueId) => new()
    {
        SessionId = sessionId,
        ConfigId = configId,
        Value = System.Text.Json.JsonSerializer.SerializeToElement(valueId),
    };

    /// <summary>Sets a <c>boolean</c> option.</summary>
    public static SetSessionConfigOptionRequest Boolean(SessionId sessionId, string configId, bool value) => new()
    {
        SessionId = sessionId,
        ConfigId = configId,
        Value = System.Text.Json.JsonSerializer.SerializeToElement(value),
        Type = "boolean",
    };
}

/// <summary>Response to <c>session/set_config_option</c>: the full, updated set of options.</summary>
public sealed record SetSessionConfigOptionResponse
{
    [JsonPropertyName("configOptions")]
    public required IReadOnlyList<ConfigOption> ConfigOptions { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}
