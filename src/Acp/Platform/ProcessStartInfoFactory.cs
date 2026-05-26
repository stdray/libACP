using System.Diagnostics;

namespace Acp.Platform;

/// <summary>
/// Creates <see cref="ProcessStartInfo"/> instances with correct cross-platform behavior.
/// Two APIs for two use cases:
/// <list type="bullet">
///   <item><see cref="CreateProcessStartInfo(string, IReadOnlyList{string}?, IReadOnlyDictionary{string, string}?, string?)"/> — argv-safe, for spawning agent processes,
///     terminal sessions, and MCP servers.</item>
///   <item><see cref="CreateShellStartInfo"/> — shell semantics, for install steps and
///     one-off commands that may use shell features (pipes, redirects, builtins).</item>
/// </list>
/// </summary>
public static class ProcessStartInfoFactory
{
    /// <summary>
    /// Creates a <see cref="ProcessStartInfo"/> for an argv-safe process launch.
    /// The command is resolved via <see cref="CommandResolver"/> and wrapped appropriately:
    /// <list type="bullet">
    ///   <item><b>.cmd/.bat on Windows</b>: Wrapped with <c>cmd.exe /d /s /c</c>.</item>
    ///   <item><b>Everything else</b>: Launched directly with <see cref="ProcessStartInfo.ArgumentList"/>.</item>
    /// </list>
    /// </summary>
    /// <param name="command">Command name or path (resolved via PATH if bare name).</param>
    /// <param name="args">Arguments to pass to the command.</param>
    /// <param name="env">Optional environment variables to set on the child process.</param>
    /// <param name="cwd">Optional working directory for the child process.</param>
    /// <returns>A configured <see cref="ProcessStartInfo"/>.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the command cannot be resolved.</exception>
    public static ProcessStartInfo CreateProcessStartInfo(
        string command,
        IReadOnlyList<string>? args = null,
        IReadOnlyDictionary<string, string>? env = null,
        string? cwd = null)
    {
        var resolved = CommandResolver.Resolve(command)
            ?? throw new FileNotFoundException(
                $"Command '{command}' not found. Searched PATH for platform-appropriate executables.", command);

        return CreateProcessStartInfo(resolved, args, env, cwd);
    }

    /// <summary>
    /// Creates a <see cref="ProcessStartInfo"/> from an already-resolved command.
    /// Use when you have a <see cref="ResolvedCommand"/> from a prior
    /// <see cref="CommandResolver.Resolve"/> call.
    /// </summary>
    public static ProcessStartInfo CreateProcessStartInfo(
        ResolvedCommand resolved,
        IReadOnlyList<string>? args = null,
        IReadOnlyDictionary<string, string>? env = null,
        string? cwd = null)
    {
        ProcessStartInfo psi;

        if (resolved.Kind == CommandKind.WindowsBatch)
        {
            // .cmd/.bat files: set FileName directly — .NET's Process.Start on Windows
            // handles batch files internally via CreateProcess. Do NOT wrap with cmd.exe
            // because that adds an extra process layer that breaks stdin piping for
            // long-running stdio-based agents.
            psi = new ProcessStartInfo
            {
                FileName = resolved.Path,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            if (args is not null)
            {
                foreach (var arg in args)
                    psi.ArgumentList.Add(arg);
            }
        }
        else
        {
            psi = new ProcessStartInfo
            {
                FileName = resolved.Path,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            if (args is not null)
            {
                foreach (var arg in args)
                    psi.ArgumentList.Add(arg);
            }
        }

        if (env is not null)
        {
            foreach (var (key, value) in env)
                psi.Environment[key] = value;
        }

        if (!string.IsNullOrEmpty(cwd))
            psi.WorkingDirectory = cwd;

        return psi;
    }

    /// <summary>
    /// Creates a <see cref="ProcessStartInfo"/> for running a shell command line.
    /// The command line is passed to the platform's shell for interpretation:
    /// <list type="bullet">
    ///   <item><b>Windows</b>: <c>cmd.exe /d /s /c "commandLine"</c></item>
    ///   <item><b>macOS / Linux</b>: <c>/bin/sh -c "commandLine"</c></item>
    /// </list>
    /// Use this for install steps, one-liners, or commands that need shell features.
    /// </summary>
    /// <param name="commandLine">The full command line to execute in a shell.</param>
    /// <param name="cwd">Optional working directory for the child process.</param>
    /// <param name="env">Optional environment variables to set on the child process.</param>
    public static ProcessStartInfo CreateShellStartInfo(
        string commandLine,
        string? cwd = null,
        IReadOnlyDictionary<string, string>? env = null)
    {
        ProcessStartInfo psi;

        if (OperatingSystem.IsWindows())
        {
            psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/d /c {commandLine}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
        }
        else
        {
            psi = new ProcessStartInfo
            {
                FileName = "/bin/sh",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add(commandLine);
        }

        if (env is not null)
        {
            foreach (var (key, value) in env)
                psi.Environment[key] = value;
        }

        if (!string.IsNullOrEmpty(cwd))
            psi.WorkingDirectory = cwd;

        return psi;
    }

    /// <summary>
    /// Quotes a string for safe inclusion in a Windows command line if it contains spaces.
    /// </summary>
    internal static string QuoteIfNeeded(string value) =>
        value.Contains(' ') || value.Contains('"')
            ? "\"" + value.Replace("\"", "\\\"") + "\""
            : value;
}
