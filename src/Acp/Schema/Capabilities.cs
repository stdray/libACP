using System.Text.Json.Serialization;

namespace Acp.Schema;

/// <summary>Capabilities advertised by a Client during initialization.</summary>
public sealed record ClientCapabilities
{
    [JsonPropertyName("fs")]
    public FileSystemCapabilities? Fs { get; init; }

    [JsonPropertyName("terminal")]
    public bool? Terminal { get; init; }

    [JsonPropertyName("session")]
    public ClientSessionCapabilities? Session { get; init; }

    [JsonPropertyName("auth")]
    public AuthCapabilities? Auth { get; init; }

    /// <summary>Elicitation support (<c>elicitation/create</c>); each mode must be advertised explicitly.</summary>
    [JsonPropertyName("elicitation")]
    public ElicitationCapabilities? Elicitation { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Session-related capabilities of the client.</summary>
public sealed record ClientSessionCapabilities
{
    [JsonPropertyName("configOptions")]
    public SessionConfigOptionsCapabilities? ConfigOptions { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Which config option kinds the client can render, beyond <c>select</c>.</summary>
public sealed record SessionConfigOptionsCapabilities
{
    /// <summary>Supplying <c>{}</c> means the client supports <c>boolean</c> config options.</summary>
    [JsonPropertyName("boolean")]
    public BooleanConfigOptionCapabilities? Boolean { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

public sealed record BooleanConfigOptionCapabilities
{
    [JsonPropertyName("_meta")] public Meta? Meta { get; init; }
}

/// <summary>Authentication capabilities of the client.</summary>
public sealed record AuthCapabilities
{
    /// <summary>Whether the client can run <c>terminal</c> auth methods (launch the agent binary with the method's args/env).</summary>
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

    [JsonPropertyName("auth")]
    public AgentAuthCapabilities? Auth { get; init; }

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

    [JsonPropertyName("delete")]
    public SessionDeleteCapabilities? Delete { get; init; }

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

public sealed record SessionDeleteCapabilities
{
    [JsonPropertyName("_meta")] public Meta? Meta { get; init; }
}

/// <summary>Authentication-related capabilities of the agent.</summary>
public sealed record AgentAuthCapabilities
{
    /// <summary>Supplying <c>{}</c> means the agent supports <c>logout</c>.</summary>
    [JsonPropertyName("logout")]
    public LogoutCapabilities? Logout { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

public sealed record LogoutCapabilities
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

    /// <summary>
    /// <c>"terminal"</c> for methods the client runs by launching the agent binary with
    /// <see cref="Args"/> / <see cref="Env"/>; omitted for agent-handled methods.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("args")]
    public IReadOnlyList<string>? Args { get; init; }

    [JsonPropertyName("env")]
    public IReadOnlyDictionary<string, string>? Env { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}
