using System.Text.Json.Serialization;

namespace Acp.Schema;

/// <summary>Parameters for the <c>session/new</c> method.</summary>
public sealed record NewSessionRequest
{
    [JsonPropertyName("cwd")]
    public required string Cwd { get; init; }

    [JsonPropertyName("mcpServers")]
    public required IReadOnlyList<McpServer> McpServers { get; init; }

    [JsonPropertyName("additionalDirectories")]
    public IReadOnlyList<string>? AdditionalDirectories { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Response to <c>session/new</c>.</summary>
public sealed record NewSessionResponse
{
    [JsonPropertyName("sessionId")]
    public required SessionId SessionId { get; init; }

    [JsonPropertyName("modes")]
    public SessionModeState? Modes { get; init; }

    /// <summary>Nested model list (Copilot nests models under <c>"models"</c>).</summary>
    [JsonPropertyName("models")]
    public AgentModelState? Models { get; init; }

    /// <summary>Flat model list (some agents use top-level <c>"availableModels"</c>).</summary>
    [JsonPropertyName("availableModels")]
    public IReadOnlyList<AgentModel>? AvailableModelsDirect { get; init; }

    [JsonPropertyName("configOptions")]
    public IReadOnlyList<ConfigOption>? ConfigOptions { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }

    /// <summary>Returns available models from whichever field is populated.</summary>
    [JsonIgnore]
    public IReadOnlyList<AgentModel> AvailableModels =>
        Models?.AvailableModels ?? AvailableModelsDirect ?? [];
}

/// <summary>Parameters for the <c>session/load</c> method.</summary>
public sealed record LoadSessionRequest
{
    [JsonPropertyName("sessionId")]
    public required SessionId SessionId { get; init; }

    [JsonPropertyName("cwd")]
    public required string Cwd { get; init; }

    [JsonPropertyName("mcpServers")]
    public required IReadOnlyList<McpServer> McpServers { get; init; }

    [JsonPropertyName("additionalDirectories")]
    public IReadOnlyList<string>? AdditionalDirectories { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Response to <c>session/load</c>. Conventionally <c>null</c> on the wire (empty result).</summary>
public sealed record LoadSessionResponse
{
    [JsonPropertyName("modes")]
    public SessionModeState? Modes { get; init; }

    [JsonPropertyName("models")]
    public AgentModelState? Models { get; init; }

    [JsonPropertyName("availableModels")]
    public IReadOnlyList<AgentModel>? AvailableModelsDirect { get; init; }

    [JsonPropertyName("configOptions")]
    public IReadOnlyList<ConfigOption>? ConfigOptions { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }

    /// <summary>Returns available models from whichever field is populated.</summary>
    [JsonIgnore]
    public IReadOnlyList<AgentModel> AvailableModels =>
        Models?.AvailableModels ?? AvailableModelsDirect ?? [];
}

/// <summary>Parameters for the <c>session/resume</c> method.</summary>
public sealed record ResumeSessionRequest
{
    [JsonPropertyName("sessionId")]
    public required SessionId SessionId { get; init; }

    [JsonPropertyName("cwd")]
    public required string Cwd { get; init; }

    [JsonPropertyName("mcpServers")]
    public required IReadOnlyList<McpServer> McpServers { get; init; }

    [JsonPropertyName("additionalDirectories")]
    public IReadOnlyList<string>? AdditionalDirectories { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Response to <c>session/resume</c>.</summary>
public sealed record ResumeSessionResponse
{
    [JsonPropertyName("modes")]
    public SessionModeState? Modes { get; init; }

    [JsonPropertyName("configOptions")]
    public IReadOnlyList<ConfigOption>? ConfigOptions { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Parameters for the <c>session/list</c> method.</summary>
public sealed record ListSessionsRequest
{
    [JsonPropertyName("cwd")]
    public string? Cwd { get; init; }

    [JsonPropertyName("additionalDirectories")]
    public IReadOnlyList<string>? AdditionalDirectories { get; init; }

    [JsonPropertyName("limit")]
    public int? Limit { get; init; }

    [JsonPropertyName("cursor")]
    public string? Cursor { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Response to <c>session/list</c>.</summary>
public sealed record ListSessionsResponse
{
    [JsonPropertyName("sessions")]
    public required IReadOnlyList<SessionInfo> Sessions { get; init; }

    [JsonPropertyName("nextCursor")]
    public string? NextCursor { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

public sealed record SessionInfo
{
    [JsonPropertyName("sessionId")]
    public required SessionId SessionId { get; init; }

    [JsonPropertyName("cwd")]
    public string? Cwd { get; init; }

    [JsonPropertyName("additionalDirectories")]
    public IReadOnlyList<string>? AdditionalDirectories { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Parameters for the <c>session/close</c> method.</summary>
public sealed record CloseSessionRequest
{
    [JsonPropertyName("sessionId")]
    public required SessionId SessionId { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Response to <c>session/close</c>.</summary>
public sealed record CloseSessionResponse
{
    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Parameters for the <c>session/delete</c> method. Requires the <c>sessionCapabilities.delete</c> capability.</summary>
public sealed record DeleteSessionRequest
{
    [JsonPropertyName("sessionId")]
    public required SessionId SessionId { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Response to <c>session/delete</c>.</summary>
public sealed record DeleteSessionResponse
{
    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Notification sent by a Client to cancel an in-flight prompt turn.</summary>
public sealed record CancelNotification
{
    [JsonPropertyName("sessionId")]
    public required SessionId SessionId { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}
