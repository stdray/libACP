using System.Diagnostics;
using System.Text;
using Acp.JsonRpc;

namespace Acp.Streaming;

/// <summary>
/// An <see cref="IMessageStream"/> that spawns an agent as a child process and communicates over
/// newline-delimited JSON on its standard I/O streams. This is the primary transport for running
/// a local ACP agent (e.g. <c>copilot --acp --stdio</c>).
/// </summary>
/// <remarks>
/// <para>Stderr is surfaced asynchronously via the <see cref="StderrReceived"/> callback.</para>
/// <para>On dispose, the process is killed (entire tree) and all resources are released.</para>
/// </remarks>
public sealed class StdioProcessTransport : IMessageStream
{
    private Process? _process;
    private NdJsonStream? _inner;
    private bool _disposed;

    /// <summary>Fires for each line written to the agent process's stderr.</summary>
    public event Action<string>? StderrReceived;

    /// <summary>Whether the agent process is currently running.</summary>
    public bool IsRunning => _process is { HasExited: false };

    /// <summary>The OS process ID of the spawned agent, or <c>null</c> if not started.</summary>
    public int? ProcessId => _process?.Id;

    /// <summary>The underlying <see cref="Process"/>, exposed for advanced scenarios (e.g. exit code).</summary>
    public Process? Process => _process;

    /// <summary>
    /// Spawn the agent process and begin reading/writing.
    /// </summary>
    /// <param name="command">The executable name or path (resolved via PATH).</param>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="env">Optional environment variables to inject into the child process.</param>
    /// <param name="workingDirectory">Optional working directory for the child process.</param>
    public void Start(string command, IReadOnlyList<string> args,
        IReadOnlyDictionary<string, string>? env = null,
        string? workingDirectory = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_process is not null)
            throw new InvalidOperationException("Transport has already been started.");

        var resolvedCommand = ResolveCommandPath(command);

        var psi = new ProcessStartInfo
        {
            FileName = resolvedCommand,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        if (env is not null)
        {
            foreach (var (key, value) in env)
                psi.Environment[key] = value;
        }

        if (!string.IsNullOrEmpty(workingDirectory))
            psi.WorkingDirectory = workingDirectory;

        _process = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start process: {command}");

        _process.StandardInput.AutoFlush = false;
        _process.EnableRaisingEvents = true;

        // Wrap stdin/stdout in NdJsonStream (don't own the streams — we manage the process)
        _inner = new NdJsonStream(
            _process.StandardOutput.BaseStream,
            _process.StandardInput.BaseStream,
            leaveOpen: true);

        // Capture stderr asynchronously
        _process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                StderrReceived?.Invoke(e.Data);
        };
        _process.BeginErrorReadLine();
    }

    /// <inheritdoc />
    public Task<JsonRpcMessage?> ReadAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_inner is null)
            throw new InvalidOperationException("Transport has not been started.");
        return _inner.ReadAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task WriteAsync(JsonRpcMessage message, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_inner is null)
            throw new InvalidOperationException("Transport has not been started.");
        return _inner.WriteAsync(message, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_inner is not null)
            await _inner.DisposeAsync().ConfigureAwait(false);

        if (_process is not null)
        {
            if (!_process.HasExited)
            {
                try { _process.Kill(entireProcessTree: true); }
                catch { /* best effort */ }
            }
            _process.Dispose();
        }
    }

    /// <summary>
    /// Resolves a command name to its full path using PATH and common extensions.
    /// .NET's Process.Start with UseShellExecute=false can fail to find executables
    /// that the shell resolves fine (e.g., WinGet-installed commands).
    /// </summary>
    private static string ResolveCommandPath(string command)
    {
        if (command.Contains(Path.DirectorySeparatorChar) ||
            command.Contains(Path.AltDirectorySeparatorChar) ||
            File.Exists(command))
            return command;

        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var dirs = pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        var extensions = OperatingSystem.IsWindows()
            ? new[] { "", ".exe", ".cmd", ".bat" }
            : new[] { "" };

        foreach (var dir in dirs)
        {
            foreach (var ext in extensions)
            {
                var candidate = Path.Combine(dir, command + ext);
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return command;
    }
}
