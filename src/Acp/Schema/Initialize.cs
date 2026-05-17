using System.Text.Json.Serialization;

namespace Acp.Schema;

/// <summary>Parameters for the <c>initialize</c> method.</summary>
public sealed record InitializeRequest
{
    [JsonPropertyName("protocolVersion")]
    public required int ProtocolVersion { get; init; }

    [JsonPropertyName("clientCapabilities")]
    public ClientCapabilities? ClientCapabilities { get; init; }

    [JsonPropertyName("clientInfo")]
    public Implementation? ClientInfo { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Response to the <c>initialize</c> method.</summary>
public sealed record InitializeResponse
{
    [JsonPropertyName("protocolVersion")]
    public required int ProtocolVersion { get; init; }

    [JsonPropertyName("agentCapabilities")]
    public AgentCapabilities? AgentCapabilities { get; init; }

    [JsonPropertyName("agentInfo")]
    public Implementation? AgentInfo { get; init; }

    [JsonPropertyName("authMethods")]
    public IReadOnlyList<AuthMethod>? AuthMethods { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Parameters for the <c>authenticate</c> method.</summary>
public sealed record AuthenticateRequest
{
    [JsonPropertyName("methodId")]
    public required string MethodId { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Response to the <c>authenticate</c> method.</summary>
public sealed record AuthenticateResponse
{
    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}
