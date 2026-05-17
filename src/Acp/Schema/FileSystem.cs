using System.Text.Json.Serialization;

namespace Acp.Schema;

/// <summary>Parameters for the <c>fs/read_text_file</c> method.</summary>
public sealed record ReadTextFileRequest
{
    [JsonPropertyName("sessionId")]
    public required SessionId SessionId { get; init; }

    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("line")]
    public int? Line { get; init; }

    [JsonPropertyName("limit")]
    public int? Limit { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Response to <c>fs/read_text_file</c>.</summary>
public sealed record ReadTextFileResponse
{
    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Parameters for the <c>fs/write_text_file</c> method.</summary>
public sealed record WriteTextFileRequest
{
    [JsonPropertyName("sessionId")]
    public required SessionId SessionId { get; init; }

    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

/// <summary>Response to <c>fs/write_text_file</c>.</summary>
public sealed record WriteTextFileResponse
{
    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}
