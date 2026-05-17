using System.Text.Json.Serialization;

namespace Acp.Schema;

/// <summary>Capabilities advertised by a Client during initialization.</summary>
public sealed record ClientCapabilities
{
    [JsonPropertyName("fs")]
    public FileSystemCapabilities? Fs { get; init; }

    [JsonPropertyName("terminal")]
    public bool? Terminal { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Whether the client supports the <c>fs/read_text_file</c> and <c>fs/write_text_file</c> methods.</summary>
public sealed record FileSystemCapabilities
{
    [JsonPropertyName("readTextFile")]
    public bool? ReadTextFile { get; init; }

    [JsonPropertyName("writeTextFile")]
    public bool? WriteTextFile { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Capabilities advertised by an Agent during initialization.</summary>
public sealed record AgentCapabilities
{
    [JsonPropertyName("loadSession")]
    public bool? LoadSession { get; init; }

    [JsonPropertyName("promptCapabilities")]
    public PromptCapabilities? PromptCapabilities { get; init; }

    [JsonPropertyName("mcpCapabilities")]
    public McpCapabilities? McpCapabilities { get; init; }

    [JsonPropertyName("sessionCapabilities")]
    public SessionCapabilities? SessionCapabilities { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Which content kinds the agent will accept inside <c>session/prompt</c> requests.</summary>
public sealed record PromptCapabilities
{
    [JsonPropertyName("image")]
    public bool? Image { get; init; }

    [JsonPropertyName("audio")]
    public bool? Audio { get; init; }

    [JsonPropertyName("embeddedContext")]
    public bool? EmbeddedContext { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>MCP transport mechanisms supported by the agent for connecting to MCP servers.</summary>
public sealed record McpCapabilities
{
    [JsonPropertyName("http")]
    public bool? Http { get; init; }

    [JsonPropertyName("sse")]
    public bool? Sse { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Optional session-lifecycle capabilities supported by the agent.</summary>
public sealed record SessionCapabilities
{
    [JsonPropertyName("list")]
    public SessionListCapabilities? List { get; init; }

    [JsonPropertyName("close")]
    public SessionCloseCapabilities? Close { get; init; }

    [JsonPropertyName("resume")]
    public SessionResumeCapabilities? Resume { get; init; }

    [JsonPropertyName("additionalDirectories")]
    public SessionAdditionalDirectoriesCapabilities? AdditionalDirectories { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

public sealed record SessionListCapabilities
{
    [JsonPropertyName("_meta")] public Meta? Meta { get; init; }
}

public sealed record SessionCloseCapabilities
{
    [JsonPropertyName("_meta")] public Meta? Meta { get; init; }
}

public sealed record SessionResumeCapabilities
{
    [JsonPropertyName("_meta")] public Meta? Meta { get; init; }
}

public sealed record SessionAdditionalDirectoriesCapabilities
{
    [JsonPropertyName("_meta")] public Meta? Meta { get; init; }
}

/// <summary>An authentication method advertised by the agent.</summary>
public sealed record AuthMethod
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}
