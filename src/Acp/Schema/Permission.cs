using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Acp.Schema.Converters;

namespace Acp.Schema;

public enum PermissionOptionKind
{
    [EnumMember(Value = "allow_once")] AllowOnce,
    [EnumMember(Value = "allow_always")] AllowAlways,
    [EnumMember(Value = "reject_once")] RejectOnce,
    [EnumMember(Value = "reject_always")] RejectAlways,
}

/// <summary>An option presented to the user for a permission request.</summary>
public sealed record PermissionOption
{
    [JsonPropertyName("optionId")]
    public required PermissionOptionId OptionId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("kind")]
    public required PermissionOptionKind Kind { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Parameters for the <c>session/request_permission</c> method.</summary>
public sealed record RequestPermissionRequest
{
    [JsonPropertyName("sessionId")]
    public required SessionId SessionId { get; init; }

    [JsonPropertyName("toolCall")]
    public required ToolCallUpdate ToolCall { get; init; }

    [JsonPropertyName("options")]
    public required IReadOnlyList<PermissionOption> Options { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Response to <c>session/request_permission</c>: the user's decision.</summary>
public sealed record RequestPermissionResponse
{
    [JsonPropertyName("outcome")]
    public required RequestPermissionOutcome Outcome { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>The outcome of a permission request: either cancelled or one of the offered options was selected.</summary>
[JsonConverter(typeof(RequestPermissionOutcomeJsonConverter))]
public abstract record RequestPermissionOutcome
{
    [JsonIgnore]
    public abstract string Outcome { get; }
}

/// <summary>The prompt turn was cancelled before the user responded.</summary>
public sealed record CancelledPermissionOutcome : RequestPermissionOutcome
{
    public override string Outcome => "cancelled";
}

/// <summary>The user selected one of the provided options.</summary>
public sealed record SelectedPermissionOutcome : RequestPermissionOutcome
{
    [JsonPropertyName("optionId")]
    public required PermissionOptionId OptionId { get; init; }

    public override string Outcome => "selected";
}
