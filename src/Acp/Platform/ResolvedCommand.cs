namespace Acp.Platform;

/// <summary>
/// Describes the kind of executable that was resolved, which determines
/// how it should be launched via <see cref="System.Diagnostics.ProcessStartInfo"/>.
/// </summary>
public enum CommandKind
{
    /// <summary>A native executable (.exe on Windows, ELF/Mach-O on Unix).</summary>
    NativeExe,

    /// <summary>A Windows batch file (.cmd or .bat). Must be launched via <c>cmd.exe /c</c>.</summary>
    WindowsBatch,

    /// <summary>An executable file on Unix (has the executable permission bit set).</summary>
    UnixExecutable,

    /// <summary>
    /// A shell script or other non-native file. On Unix this is valid (shebang-based).
    /// On Windows, extensionless shell scripts are skipped during resolution.
    /// </summary>
    ShellScript,
}

/// <summary>
/// The result of resolving a command name to an executable path.
/// </summary>
/// <param name="Path">Full filesystem path to the resolved executable.</param>
/// <param name="Kind">What kind of executable was found.</param>
/// <param name="OriginalName">The command name that was originally requested.</param>
public sealed record ResolvedCommand(string Path, CommandKind Kind, string OriginalName);
