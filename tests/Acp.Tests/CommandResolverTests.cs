using System.Diagnostics;
using System.Runtime.InteropServices;
using Acp.Platform;

namespace Acp.Tests;

public class CommandResolverTests : IDisposable
{
    private readonly string _tempDir;

    public CommandResolverTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "acp-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort cleanup */ }
    }

    // ── Resolution priority tests ──────────────────────────────────

    [Fact]
    public void Resolve_PrefersDotExeOverDotCmd()
    {
        if (!OperatingSystem.IsWindows()) return;

        CreateFile("testcmd.exe");
        CreateFile("testcmd.cmd");
        CreateFile("testcmd.bat");

        var result = ResolveWithTempPath("testcmd");

        Assert.NotNull(result);
        Assert.Equal(CommandKind.NativeExe, result.Kind);
        Assert.EndsWith(".exe", result.Path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_PrefersDotCmdOverExtensionless()
    {
        if (!OperatingSystem.IsWindows()) return;

        CreateFile("testcmd.cmd");
        CreateShebangFile("testcmd"); // extensionless bash script

        var result = ResolveWithTempPath("testcmd");

        Assert.NotNull(result);
        Assert.Equal(CommandKind.WindowsBatch, result.Kind);
        Assert.EndsWith(".cmd", result.Path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_SkipsShebangFilesOnWindows()
    {
        if (!OperatingSystem.IsWindows()) return;

        // Only a shebang file, no .exe/.cmd/.bat
        CreateShebangFile("bashonly");

        var result = ResolveWithTempPath("bashonly");

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_FindsDotBatWhenNoDotExeOrDotCmd()
    {
        if (!OperatingSystem.IsWindows()) return;

        CreateFile("batcmd.bat");

        var result = ResolveWithTempPath("batcmd");

        Assert.NotNull(result);
        Assert.Equal(CommandKind.WindowsBatch, result.Kind);
    }

    // ── Shell start info tests ─────────────────────────────────────

    [Fact]
    public void CreateShellStartInfo_UsesCorrectShell()
    {
        var psi = ProcessStartInfoFactory.CreateShellStartInfo("echo hello");

        if (OperatingSystem.IsWindows())
        {
            Assert.Equal("cmd.exe", psi.FileName);
            Assert.Contains("/c", psi.Arguments);
            Assert.Contains("echo hello", psi.Arguments);
        }
        else
        {
            Assert.Equal("/bin/sh", psi.FileName);
            Assert.Contains("-c", psi.ArgumentList);
            Assert.Contains("echo hello", psi.ArgumentList);
        }
    }

    [Fact]
    public void CreateShellStartInfo_SetsCwd()
    {
        var psi = ProcessStartInfoFactory.CreateShellStartInfo("echo test", cwd: _tempDir);
        Assert.Equal(_tempDir, psi.WorkingDirectory);
    }

    [Fact]
    public void CreateShellStartInfo_SetsEnvironment()
    {
        var env = new Dictionary<string, string> { ["MY_VAR"] = "my_value" };
        var psi = ProcessStartInfoFactory.CreateShellStartInfo("echo test", env: env);
        Assert.Equal("my_value", psi.Environment["MY_VAR"]);
    }

    // ── Process start info tests ───────────────────────────────────

    [Fact]
    public void CreateProcessStartInfo_LaunchesCmdDirectly()
    {
        if (!OperatingSystem.IsWindows()) return;

        CreateFile("wrapped.cmd");

        var oldPath = Environment.GetEnvironmentVariable("PATH");
        try
        {
            Environment.SetEnvironmentVariable("PATH", _tempDir + Path.PathSeparator + oldPath);

            var psi = ProcessStartInfoFactory.CreateProcessStartInfo("wrapped", ["arg1", "arg2"]);

            // .cmd files are set as FileName directly — .NET handles them via CreateProcess
            Assert.EndsWith("wrapped.cmd", psi.FileName, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("arg1", psi.ArgumentList);
            Assert.Contains("arg2", psi.ArgumentList);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", oldPath);
        }
    }

    [Fact]
    public void CreateProcessStartInfo_DirectLaunchForExe()
    {
        if (!OperatingSystem.IsWindows()) return;

        CreateFile("myagent.exe");

        var oldPath = Environment.GetEnvironmentVariable("PATH");
        try
        {
            Environment.SetEnvironmentVariable("PATH", _tempDir + Path.PathSeparator + oldPath);

            var psi = ProcessStartInfoFactory.CreateProcessStartInfo("myagent", ["--version"]);

            Assert.EndsWith("myagent.exe", psi.FileName, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("--version", psi.ArgumentList);
            // Should NOT be wrapped in cmd.exe
            Assert.NotEqual("cmd.exe", psi.FileName);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", oldPath);
        }
    }

    [Fact]
    public void CreateProcessStartInfo_ThrowsForMissingCommand()
    {
        Assert.Throws<FileNotFoundException>(() =>
            ProcessStartInfoFactory.CreateProcessStartInfo("nonexistent_command_xyz_123"));
    }

    [Fact]
    public void CreateProcessStartInfo_SetsEnvAndCwd()
    {
        // Use a known-available command
        var cmd = OperatingSystem.IsWindows() ? "cmd" : "sh";
        var env = new Dictionary<string, string> { ["TEST_KEY"] = "test_val" };

        var psi = ProcessStartInfoFactory.CreateProcessStartInfo(cmd, env: env, cwd: _tempDir);

        Assert.Equal("test_val", psi.Environment["TEST_KEY"]);
        Assert.Equal(_tempDir, psi.WorkingDirectory);
    }

    // ── Explicit extension tests ───────────────────────────────────

    [Fact]
    public void Resolve_ExplicitExtensionDoesNotAppendMore()
    {
        if (!OperatingSystem.IsWindows()) return;

        // Create npm.cmd but NOT npm.cmd.exe
        CreateFile("npm.cmd");

        var result = ResolveWithTempPath("npm.cmd");

        Assert.NotNull(result);
        Assert.Equal(CommandKind.WindowsBatch, result.Kind);
        Assert.EndsWith("npm.cmd", result.Path, StringComparison.OrdinalIgnoreCase);
    }

    // ── Absolute path tests ────────────────────────────────────────

    [Fact]
    public void Resolve_AbsolutePathValidatedDirectly()
    {
        var filePath = CreateFile("absolute.exe");

        var result = CommandResolver.Resolve(filePath);

        Assert.NotNull(result);
        Assert.Equal(Path.GetFullPath(filePath), result.Path);
    }

    [Fact]
    public void Resolve_AbsolutePathReturnsNullIfMissing()
    {
        var result = CommandResolver.Resolve(Path.Combine(_tempDir, "does_not_exist.exe"));
        Assert.Null(result);
    }

    // ── IsAvailable tests ──────────────────────────────────────────

    [Fact]
    public void IsAvailable_ReturnsFalseForNonexistent()
    {
        Assert.False(CommandResolver.IsAvailable("nonexistent_command_xyz_123"));
    }

    [Fact]
    public void IsAvailable_ReturnsTrueForNullOrEmpty()
    {
        Assert.False(CommandResolver.IsAvailable(""));
        Assert.False(CommandResolver.IsAvailable("   "));
    }

    // ── Smoke tests (real commands) ────────────────────────────────

    [Fact]
    public void Resolve_FindsGit()
    {
        var result = CommandResolver.Resolve("git");
        // git should be available in most dev environments and CI
        if (result is not null)
        {
            Assert.Equal("git", result.OriginalName);
            Assert.True(File.Exists(result.Path));
        }
    }

    [Fact]
    public void Resolve_FindsDotnet()
    {
        var result = CommandResolver.Resolve("dotnet");
        Assert.NotNull(result);
        Assert.Equal("dotnet", result.OriginalName);
        Assert.True(File.Exists(result.Path));
    }

    // ── PATH ordering tests ────────────────────────────────────────

    [Fact]
    public void Resolve_RespectsPathOrdering()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dir1 = Path.Combine(_tempDir, "first");
        var dir2 = Path.Combine(_tempDir, "second");
        Directory.CreateDirectory(dir1);
        Directory.CreateDirectory(dir2);

        File.WriteAllText(Path.Combine(dir1, "ordercmd.exe"), "first");
        File.WriteAllText(Path.Combine(dir2, "ordercmd.exe"), "second");

        var oldPath = Environment.GetEnvironmentVariable("PATH");
        try
        {
            // dir1 comes first in PATH
            Environment.SetEnvironmentVariable("PATH",
                dir1 + Path.PathSeparator + dir2 + Path.PathSeparator + oldPath);

            var result = CommandResolver.Resolve("ordercmd");

            Assert.NotNull(result);
            Assert.StartsWith(dir1, result.Path);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", oldPath);
        }
    }

    // ── Shebang detection tests ────────────────────────────────────

    [Fact]
    public void IsShebangFile_DetectsShebang()
    {
        var path = CreateShebangFile("shebangtest");
        Assert.True(CommandResolver.IsShebangFile(path));
    }

    [Fact]
    public void IsShebangFile_ReturnsFalseForBinary()
    {
        var path = CreateFile("binarytest");
        Assert.False(CommandResolver.IsShebangFile(path));
    }

    // ── Helpers ────────────────────────────────────────────────────

    private string CreateFile(string name, byte[]? content = null)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllBytes(path, content ?? new byte[] { 0x4D, 0x5A, 0x00 }); // MZ header stub
        return path;
    }

    private string CreateShebangFile(string name)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, "#!/bin/bash\necho hello\n");
        return path;
    }

    /// <summary>
    /// Temporarily prepends _tempDir to PATH, resolves, then restores.
    /// </summary>
    private ResolvedCommand? ResolveWithTempPath(string command)
    {
        var oldPath = Environment.GetEnvironmentVariable("PATH");
        try
        {
            Environment.SetEnvironmentVariable("PATH", _tempDir + Path.PathSeparator + oldPath);
            return CommandResolver.Resolve(command);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", oldPath);
        }
    }
}
