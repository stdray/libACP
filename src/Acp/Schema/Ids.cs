using System.Text.Json.Serialization;
using Acp.Schema.Converters;

namespace Acp.Schema;

/// <summary>An ACP session id. Wraps a string with type safety so it cannot be confused with other ids.</summary>
[JsonConverter(typeof(StringIdConverter<SessionId>.Json))]
public readonly record struct SessionId(string Value)
{
    public override string ToString() => Value;
    public static implicit operator string(SessionId id) => id.Value;
}

/// <summary>An ACP tool call id.</summary>
[JsonConverter(typeof(StringIdConverter<ToolCallId>.Json))]
public readonly record struct ToolCallId(string Value)
{
    public override string ToString() => Value;
    public static implicit operator string(ToolCallId id) => id.Value;
}

/// <summary>A terminal id returned by <c>terminal/create</c>.</summary>
[JsonConverter(typeof(StringIdConverter<TerminalId>.Json))]
public readonly record struct TerminalId(string Value)
{
    public override string ToString() => Value;
    public static implicit operator string(TerminalId id) => id.Value;
}

/// <summary>A session mode id (e.g. "ask", "code", "architect").</summary>
[JsonConverter(typeof(StringIdConverter<SessionModeId>.Json))]
public readonly record struct SessionModeId(string Value)
{
    public override string ToString() => Value;
    public static implicit operator string(SessionModeId id) => id.Value;
}

/// <summary>A permission option id.</summary>
[JsonConverter(typeof(StringIdConverter<PermissionOptionId>.Json))]
public readonly record struct PermissionOptionId(string Value)
{
    public override string ToString() => Value;
    public static implicit operator string(PermissionOptionId id) => id.Value;
}
