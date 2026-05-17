using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Acp.Schema;

/// <summary>Reasons why an agent stops processing a prompt turn.</summary>
public enum StopReason
{
    [EnumMember(Value = "end_turn")] EndTurn,
    [EnumMember(Value = "max_tokens")] MaxTokens,
    [EnumMember(Value = "max_turn_requests")] MaxTurnRequests,
    [EnumMember(Value = "refusal")] Refusal,
    [EnumMember(Value = "cancelled")] Cancelled,
}

/// <summary>Parameters for the <c>session/prompt</c> method.</summary>
public sealed record PromptRequest
{
    [JsonPropertyName("sessionId")]
    public required SessionId SessionId { get; init; }

    [JsonPropertyName("prompt")]
    public required IReadOnlyList<ContentBlock> Prompt { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Response to <c>session/prompt</c>.</summary>
public sealed record PromptResponse
{
    [JsonPropertyName("stopReason")]
    public required StopReason StopReason { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}
