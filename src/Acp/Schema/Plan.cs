using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Acp.Schema;

/// <summary>An execution plan reported by the agent.</summary>
public sealed record Plan
{
    [JsonPropertyName("entries")]
    public required IReadOnlyList<PlanEntry> Entries { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

public sealed record PlanEntry
{
    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("priority")]
    public required PlanEntryPriority Priority { get; init; }

    [JsonPropertyName("status")]
    public required PlanEntryStatus Status { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

public enum PlanEntryPriority
{
    [EnumMember(Value = "high")] High,
    [EnumMember(Value = "medium")] Medium,
    [EnumMember(Value = "low")] Low,
}

public enum PlanEntryStatus
{
    [EnumMember(Value = "pending")] Pending,
    [EnumMember(Value = "in_progress")] InProgress,
    [EnumMember(Value = "completed")] Completed,
}
