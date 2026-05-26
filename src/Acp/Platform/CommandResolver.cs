namespace Acp.Platform;

/// <summary>
/// Cross-platform command resolution. Finds executables on PATH with correct
/// platform-specific behavior:
/// <list type="bullet">
///   <item><b>Windows</b>: Searches <c>.exe</c>, <c>.cmd</c>, <c>.bat</c> before extensionless.
///     Skips extensionless files that start with <c>#!</c> (bash/shell scripts from
///     Node.js, Git for Windows, etc.).</item>
///   <item><b>macOS / Linux</b>: Requires the executable permission bit.
///     Does NOT reject scripts — shebang-based executables are valid on Unix.</item>
/// </list>
/// </summary>
public static class CommandResolver
{
    /// <summary>
    /// Resolves a command name to its full executable path, or <c>null</c> if not found.
    /// </summary>
    /// <param name="command">
    /// A bare command name (e.g. <c>"npx"</c>) or an absolute/relative path.
    /// If the command already contains a directory separator, it is validated directly
    /// without searching PATH.
    /// </param>
    public static ResolvedCommand? Resolve(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return null;

        // If command has a directory component, validate it directly without searching PATH.
        if (HasDirectoryComponent(command))
            return ValidateExplicitPath(command);

        // If command has an explicit extension, search PATH for that exact filename.
        if (HasExplicitExtension(command))
            return SearchPathForExact(command);

        // Bare command name — search PATH with platform-specific extension priority.
        return SearchPath(command);
    }

    /// <summary>
    /// Returns <c>true</c> if the command can be resolved to a valid executable.
    /// </summary>
    public static bool IsAvailable(string command) => Resolve(command) is not null;

    // ── PATH search ────────────────────────────────────────────────

    private static ResolvedCommand? SearchPath(string command)
    {
        var dirs = GetPathDirectories();

        if (OperatingSystem.IsWindows())
        {
            // Windows: search with extensions first (.exe > .cmd > .bat), then extensionless.
            // Skip extensionless files that are shell scripts (start with #!).
            foreach (var dir in dirs)
            {
                var exe = Path.Combine(dir, command + ".exe");
                if (File.Exists(exe))
                    return new ResolvedCommand(exe, CommandKind.NativeExe, command);

                var cmd = Path.Combine(dir, command + ".cmd");
                if (File.Exists(cmd))
                    return new ResolvedCommand(cmd, CommandKind.WindowsBatch, command);

                var bat = Path.Combine(dir, command + ".bat");
                if (File.Exists(bat))
                    return new ResolvedCommand(bat, CommandKind.WindowsBatch, command);
            }

            // Finally, extensionless — but skip shell scripts
            foreach (var dir in dirs)
            {
                var bare = Path.Combine(dir, command);
                if (File.Exists(bare) && !IsShebangFile(bare))
                    return new ResolvedCommand(bare, CommandKind.NativeExe, command);
            }
        }
        else
        {
            // Unix: search for the command name as-is. Require executable permission.
            foreach (var dir in dirs)
            {
                var candidate = Path.Combine(dir, command);
                if (File.Exists(candidate) && IsUnixExecutable(candidate))
                {
                    var kind = IsShebangFile(candidate)
                        ? CommandKind.ShellScript
                        : CommandKind.UnixExecutable;
                    return new ResolvedCommand(candidate, kind, command);
                }
            }
        }

        return null;
    }

    private static ResolvedCommand? SearchPathForExact(string command)
    {
        var dirs = GetPathDirectories();

        foreach (var dir in dirs)
        {
            var candidate = Path.Combine(dir, command);
            if (!File.Exists(candidate)) continue;

            var kind = ClassifyFile(candidate);
            if (kind is not null)
                return new ResolvedCommand(candidate, kind.Value, command);
        }

        return null;
    }

    private static ResolvedCommand? ValidateExplicitPath(string path)
    {
        if (!File.Exists(path))
            return null;

        var kind = ClassifyFile(path);
        return kind is not null
            ? new ResolvedCommand(Path.GetFullPath(path), kind.Value, Path.GetFileName(path))
            : null;
    }

    // ── File classification ────────────────────────────────────────

    private static CommandKind? ClassifyFile(string path)
    {
        var ext = Path.GetExtension(path);

        if (OperatingSystem.IsWindows())
        {
            if (ext.Equals(".exe", StringComparison.OrdinalIgnoreCase))
                return CommandKind.NativeExe;
            if (ext.Equals(".cmd", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".bat", StringComparison.OrdinalIgnoreCase))
                return CommandKind.WindowsBatch;
            if (string.IsNullOrEmpty(ext))
                return IsShebangFile(path) ? null : CommandKind.NativeExe;
            return null;
        }
        else
        {
            if (!IsUnixExecutable(path))
                return null;
            return IsShebangFile(path) ? CommandKind.ShellScript : CommandKind.UnixExecutable;
        }
    }

    // ── Platform helpers ───────────────────────────────────────────

    /// <summary>
    /// Checks whether a file starts with <c>#!</c> (a Unix shebang).
    /// On Windows, these are bash/shell scripts (from Node.js, Git for Windows)
    /// that cannot be executed directly by <c>Process.Start</c>.
    /// </summary>
    internal static bool IsShebangFile(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            Span<byte> header = stackalloc byte[2];
            return fs.Read(header) == 2 && header[0] == '#' && header[1] == '!';
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// On Unix, checks whether a file has the executable permission bit set.
    /// </summary>
    internal static bool IsUnixExecutable(string path)
    {
        if (OperatingSystem.IsWindows())
            return true;

        try
        {
            var mode = File.GetUnixFileMode(path);
            return (mode & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool HasDirectoryComponent(string command) =>
        command.Contains(Path.DirectorySeparatorChar) ||
        command.Contains(Path.AltDirectorySeparatorChar);

    private static bool HasExplicitExtension(string command)
    {
        var ext = Path.GetExtension(command);
        return !string.IsNullOrEmpty(ext);
    }

    private static string[] GetPathDirectories()
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        return pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
    }
}
