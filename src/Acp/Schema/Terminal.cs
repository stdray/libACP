using System.Text.Json.Serialization;

namespace Acp.Schema;

/// <summary>Parameters for <c>terminal/create</c>.</summary>
public sealed record CreateTerminalRequest
{
    [JsonPropertyName("sessionId")]
    public required SessionId SessionId { get; init; }

    [JsonPropertyName("command")]
    public required string Command { get; init; }

    [JsonPropertyName("args")]
    public IReadOnlyList<string>? Args { get; init; }

    [JsonPropertyName("env")]
    public IReadOnlyList<EnvVariable>? Env { get; init; }

    [JsonPropertyName("cwd")]
    public string? Cwd { get; init; }

    [JsonPropertyName("outputByteLimit")]
    public long? OutputByteLimit { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

public sealed record CreateTerminalResponse
{
    [JsonPropertyName("terminalId")]
    public required TerminalId TerminalId { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

public sealed record TerminalOutputRequest
{
    [JsonPropertyName("sessionId")]
    public required SessionId SessionId { get; init; }

    [JsonPropertyName("terminalId")]
    public required TerminalId TerminalId { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

public sealed record TerminalOutputResponse
{
    [JsonPropertyName("output")]
    public required string Output { get; init; }

    [JsonPropertyName("truncated")]
    public required bool Truncated { get; init; }

    [JsonPropertyName("exitStatus")]
    public TerminalExitStatus? ExitStatus { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

public sealed record ReleaseTerminalRequest
{
    [JsonPropertyName("sessionId")]
    public required SessionId SessionId { get; init; }

    [JsonPropertyName("terminalId")]
    public required TerminalId TerminalId { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

public sealed record ReleaseTerminalResponse
{
    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

public sealed record WaitForTerminalExitRequest
{
    [JsonPropertyName("sessionId")]
    public required SessionId SessionId { get; init; }

    [JsonPropertyName("terminalId")]
    public required TerminalId TerminalId { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

public sealed record WaitForTerminalExitResponse
{
    [JsonPropertyName("exitCode")]
    public int? ExitCode { get; init; }

    [JsonPropertyName("signal")]
    public string? Signal { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

public sealed record KillTerminalRequest
{
    [JsonPropertyName("sessionId")]
    public required SessionId SessionId { get; init; }

    [JsonPropertyName("terminalId")]
    public required TerminalId TerminalId { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

public sealed record KillTerminalResponse
{
    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}

public sealed record TerminalExitStatus
{
    [JsonPropertyName("exitCode")]
    public int? ExitCode { get; init; }

    [JsonPropertyName("signal")]
    public string? Signal { get; init; }

    [JsonPropertyName("_meta")]
    public Meta? Meta { get; init; }
}
