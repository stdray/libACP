using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Acp.Schema.Converters;

namespace Acp.Schema;

/// <summary>Categories of tools that can be invoked.</summary>
public enum ToolKind
{
    [EnumMember(Value = "read")] Read,
    [EnumMember(Value = "edit")] Edit,
    [EnumMember(Value = "delete")] Delete,
    [EnumMember(Value = "move")] Move,
    [EnumMember(Value = "search")] Search,
    [EnumMember(Value = "execute")] Execute,
    [EnumMember(Value = "think")] Think,
    [EnumMember(Value = "fetch")] Fetch,
    [EnumMember(Value = "switch_mode")] SwitchMode,
    [EnumMember(Value = "other")] Other,
}

/// <summary>Execution status of a tool call.</summary>
public enum ToolCallStatus
{
    [EnumMember(Value = "pending")] Pending,
    [EnumMember(Value = "in_progress")] InProgress,
    [EnumMember(Value = "completed")] Completed,
    [EnumMember(Value = "failed")] Failed,
}

/// <summary>A file location associated with a tool call (for "follow the agent" UX).</summary>
public sealed record ToolCallLocation
{
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("line")]
    public int? Line { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Initial tool-call notification.</summary>
public sealed record ToolCall
{
    [JsonPropertyName("toolCallId")]
    public required ToolCallId ToolCallId { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("kind")]
    public ToolKind? Kind { get; init; }

    [JsonPropertyName("status")]
    public ToolCallStatus? Status { get; init; }

    [JsonPropertyName("content")]
    public IReadOnlyList<ToolCallContent>? Content { get; init; }

    [JsonPropertyName("locations")]
    public IReadOnlyList<ToolCallLocation>? Locations { get; init; }

    [JsonPropertyName("rawInput")]
    public JsonElement? RawInput { get; init; }

    [JsonPropertyName("rawOutput")]
    public JsonElement? RawOutput { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Update to an existing tool call. All fields except <see cref="ToolCallId"/> are optional.</summary>
public sealed record ToolCallUpdate
{
    [JsonPropertyName("toolCallId")]
    public required ToolCallId ToolCallId { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("kind")]
    public ToolKind? Kind { get; init; }

    [JsonPropertyName("status")]
    public ToolCallStatus? Status { get; init; }

    [JsonPropertyName("content")]
    public IReadOnlyList<ToolCallContent>? Content { get; init; }

    [JsonPropertyName("locations")]
    public IReadOnlyList<ToolCallLocation>? Locations { get; init; }

    [JsonPropertyName("rawInput")]
    public JsonElement? RawInput { get; init; }

    [JsonPropertyName("rawOutput")]
    public JsonElement? RawOutput { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Content produced by a tool call: standard content, a diff, or an embedded terminal.</summary>
[JsonConverter(typeof(ToolCallContentJsonConverter))]
public abstract record ToolCallContent
{
    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }

    [JsonIgnore]
    public abstract string Type { get; }
}

public sealed record ToolCallContentBlock : ToolCallContent
{
    [JsonPropertyName("content")]
    public required ContentBlock Content { get; init; }

    public override string Type => "content";
}

public sealed record ToolCallContentDiff : ToolCallContent
{
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("oldText")]
    public string? OldText { get; init; }

    [JsonPropertyName("newText")]
    public required string NewText { get; init; }

    public override string Type => "diff";
}

public sealed record ToolCallContentTerminal : ToolCallContent
{
    [JsonPropertyName("terminalId")]
    public required TerminalId TerminalId { get; init; }

    public override string Type => "terminal";
}
