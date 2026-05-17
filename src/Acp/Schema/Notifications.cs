using System.Text.Json.Serialization;
using Acp.Schema.Converters;

namespace Acp.Schema;

/// <summary>An available command exposed by the agent for slash-command UX.</summary>
public sealed record AvailableCommand
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("input")]
    public AvailableCommandInput? Input { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Input descriptor for an available command (only the unstructured variant is modelled here).</summary>
public sealed record AvailableCommandInput
{
    [JsonPropertyName("hint")]
    public string? Hint { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>The <c>session/update</c> notification envelope sent by the agent to the client.</summary>
public sealed record SessionNotification
{
    [JsonPropertyName("sessionId")]
    public required SessionId SessionId { get; init; }

    [JsonPropertyName("update")]
    public required SessionUpdate Update { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>
/// Updates streamed during a prompt turn (or session load replay). Discriminated by the
/// <c>sessionUpdate</c> property.
/// </summary>
[JsonConverter(typeof(SessionUpdateJsonConverter))]
public abstract record SessionUpdate
{
    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }

    /// <summary>The wire <c>sessionUpdate</c> discriminator value.</summary>
    [JsonIgnore]
    public abstract string SessionUpdateKind { get; }
}

/// <summary>Common shape of the chunk-style updates (user/agent message, agent thoughts).</summary>
public abstract record ContentChunkUpdate : SessionUpdate
{
    [JsonPropertyName("content")]
    public required ContentBlock Content { get; init; }
}

public sealed record UserMessageChunk : ContentChunkUpdate
{
    public override string SessionUpdateKind => "user_message_chunk";
}

public sealed record AgentMessageChunk : ContentChunkUpdate
{
    public override string SessionUpdateKind => "agent_message_chunk";
}

public sealed record AgentThoughtChunk : ContentChunkUpdate
{
    public override string SessionUpdateKind => "agent_thought_chunk";
}

/// <summary>A new tool call has been initiated.</summary>
public sealed record ToolCallStartUpdate : SessionUpdate
{
    [JsonPropertyName("toolCallId")] public required ToolCallId ToolCallId { get; init; }
    [JsonPropertyName("title")] public required string Title { get; init; }
    [JsonPropertyName("kind")] public ToolKind? Kind { get; init; }
    [JsonPropertyName("status")] public ToolCallStatus? Status { get; init; }
    [JsonPropertyName("content")] public IReadOnlyList<ToolCallContent>? Content { get; init; }
    [JsonPropertyName("locations")] public IReadOnlyList<ToolCallLocation>? Locations { get; init; }
    [JsonPropertyName("rawInput")] public System.Text.Json.JsonElement? RawInput { get; init; }
    [JsonPropertyName("rawOutput")] public System.Text.Json.JsonElement? RawOutput { get; init; }

    public override string SessionUpdateKind => "tool_call";
}

/// <summary>An update to an existing tool call.</summary>
public sealed record ToolCallUpdateUpdate : SessionUpdate
{
    [JsonPropertyName("toolCallId")] public required ToolCallId ToolCallId { get; init; }
    [JsonPropertyName("title")] public string? Title { get; init; }
    [JsonPropertyName("kind")] public ToolKind? Kind { get; init; }
    [JsonPropertyName("status")] public ToolCallStatus? Status { get; init; }
    [JsonPropertyName("content")] public IReadOnlyList<ToolCallContent>? Content { get; init; }
    [JsonPropertyName("locations")] public IReadOnlyList<ToolCallLocation>? Locations { get; init; }
    [JsonPropertyName("rawInput")] public System.Text.Json.JsonElement? RawInput { get; init; }
    [JsonPropertyName("rawOutput")] public System.Text.Json.JsonElement? RawOutput { get; init; }

    public override string SessionUpdateKind => "tool_call_update";
}

/// <summary>The agent's plan for the current turn.</summary>
public sealed record PlanUpdate : SessionUpdate
{
    [JsonPropertyName("entries")]
    public required IReadOnlyList<PlanEntry> Entries { get; init; }

    public override string SessionUpdateKind => "plan";
}

/// <summary>The set of available commands has changed.</summary>
public sealed record AvailableCommandsUpdate : SessionUpdate
{
    [JsonPropertyName("availableCommands")]
    public required IReadOnlyList<AvailableCommand> AvailableCommands { get; init; }

    public override string SessionUpdateKind => "available_commands_update";
}

/// <summary>The current mode of the session has changed.</summary>
public sealed record CurrentModeUpdate : SessionUpdate
{
    [JsonPropertyName("modeId")]
    public required SessionModeId ModeId { get; init; }

    public override string SessionUpdateKind => "current_mode_update";
}

/// <summary>Session metadata (title, timestamps) has been updated.</summary>
public sealed record SessionInfoUpdate : SessionUpdate
{
    [JsonPropertyName("title")] public string? Title { get; init; }
    [JsonPropertyName("updatedAt")] public string? UpdatedAt { get; init; }
    [JsonPropertyName("metadata")] public System.Text.Json.JsonElement? Metadata { get; init; }

    public override string SessionUpdateKind => "session_info_update";
}

/// <summary>Signals the end of an agent turn, optionally including token usage.</summary>
public sealed record EndTurnUpdate : SessionUpdate
{
    [JsonPropertyName("usage")]
    public System.Text.Json.JsonElement? Usage { get; init; }

    public override string SessionUpdateKind => "end_turn";
}

/// <summary>A file diff produced by the agent during a tool call.</summary>
public sealed record DiffUpdate : SessionUpdate
{
    [JsonPropertyName("path")] public string? Path { get; init; }
    [JsonPropertyName("content")] public string? Content { get; init; }
    [JsonPropertyName("diff")] public string? Diff { get; init; }

    public override string SessionUpdateKind => "diff";
}
