using System.Text.Json.Serialization;
using Acp.Schema.Converters;

namespace Acp.Schema;

/// <summary>An environment variable name/value pair.</summary>
public sealed record EnvVariable
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("value")]
    public required string Value { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>
/// MCP server configuration passed to <c>session/new</c> / <c>session/load</c>.
/// </summary>
/// <remarks>
/// Three transports are supported: stdio (always), HTTP and SSE (only when the agent advertises
/// the corresponding <see cref="McpCapabilities"/>).
/// </remarks>
[JsonConverter(typeof(McpServerJsonConverter))]
public abstract record McpServer
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }

    /// <summary>The wire <c>type</c> discriminator value. <c>null</c> indicates legacy stdio (no <c>type</c> field).</summary>
    [JsonIgnore]
    public abstract string? Type { get; }
}

/// <summary>An MCP server reachable via stdio (subprocess).</summary>
public sealed record McpServerStdio : McpServer
{
    [JsonPropertyName("command")]
    public required string Command { get; init; }

    [JsonPropertyName("args")]
    public IReadOnlyList<string>? Args { get; init; }

    [JsonPropertyName("env")]
    public IReadOnlyList<EnvVariable>? Env { get; init; }

    /// <summary>For stdio servers the <c>type</c> field is omitted on the wire (matches MCP convention).</summary>
    public override string? Type => "stdio";
}

/// <summary>An HTTP-transport MCP server. Requires the agent's <c>mcpCapabilities.http</c> capability.</summary>
public sealed record McpServerHttp : McpServer
{
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("headers")]
    public IReadOnlyList<HttpHeader>? Headers { get; init; }

    public override string? Type => "http";
}

/// <summary>An SSE-transport MCP server. Requires the agent's <c>mcpCapabilities.sse</c> capability.</summary>
public sealed record McpServerSse : McpServer
{
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("headers")]
    public IReadOnlyList<HttpHeader>? Headers { get; init; }

    public override string? Type => "sse";
}

public sealed record HttpHeader
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("value")]
    public required string Value { get; init; }
}
