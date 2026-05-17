using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acp.Schema;

/// <summary>
/// Bag of arbitrary metadata reserved for use by clients and agents to attach additional
/// information to a message. The protocol explicitly forbids implementations from making
/// assumptions about values at these keys.
/// </summary>
/// <remarks>See <see href="https://agentclientprotocol.com/protocol/extensibility"/>.</remarks>
public sealed class Meta : Dictionary<string, JsonElement>
{
    public Meta() { }
    public Meta(IDictionary<string, JsonElement> source) : base(source) { }
}

/// <summary>
/// Information about an agent or client implementation: name, optional human-readable title and
/// version. Sent during initialization to identify the peer.
/// </summary>
public sealed record Implementation
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>
/// Optional MCP-style annotations attached to content. See
/// <see href="https://modelcontextprotocol.io/specification/2025-06-18/server/resources#annotations"/>.
/// </summary>
public sealed record Annotations
{
    [JsonPropertyName("audience")]
    public IReadOnlyList<Role>? Audience { get; init; }

    [JsonPropertyName("priority")]
    public double? Priority { get; init; }

    [JsonPropertyName("lastModified")]
    public string? LastModified { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>The intended audience for a piece of content.</summary>
public enum Role
{
    User,
    Assistant,
}
