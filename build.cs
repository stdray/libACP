#:sdk Cake.Sdk@6.2.0
#:property Nullable=enable

using System.Text.Json;

// Build entry point: ./build.ps1 or ./build.sh (they restore .config/dotnet-tools.json first).
// The package version comes from GitVersion (GitVersion.yml), never from the csproj.

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var solution = "./Acp.slnx";
var testProject = "./tests/Acp.Tests/Acp.Tests.csproj";
var libraryProject = "./src/Acp/Acp.csproj";
var artifacts = "./artifacts";

GitVersionInfo? version = null;

DotNetMSBuildSettings VersionProperties() => new DotNetMSBuildSettings()
	.WithProperty("Version", version!.FullSemVer)
	.WithProperty("InformationalVersion", $"{version.FullSemVer} ({version.ShortSha}, {version.CommitDate})");

// dotnet-gitversion is a local tool, so it is run through `dotnet tool run` rather than Cake's
// GitVersion alias (which only probes PATH for a global install). The repo path is passed
// explicitly: without it GitVersion fails to find .git when launched from Git Bash on Windows.
GitVersionInfo RunGitVersion()
{
	var repoRoot = MakeAbsolute(Directory(".")).FullPath;
	var exit = StartProcess("dotnet", new ProcessSettings
	{
		Arguments = new ProcessArgumentBuilder()
			.Append("tool").Append("run").Append("dotnet-gitversion")
			.AppendQuoted(repoRoot)
			.Append("/output").Append("json"),
		RedirectStandardOutput = true,
	}, out var output);
	var json = string.Join("\n", output);
	if (exit != 0)
		throw new CakeException($"dotnet-gitversion failed with exit code {exit}:\n{json}");
	return JsonSerializer.Deserialize<GitVersionInfo>(json)
		?? throw new CakeException("dotnet-gitversion returned no output.");
}

void Run(string exe, string args)
{
	var exit = StartProcess(exe, new ProcessSettings { Arguments = args });
	if (exit != 0)
		throw new CakeException($"{exe} {args} failed with exit code {exit}");
}

// ─── Tasks ───

Task("Clean")
	.Does(() =>
	{
		CleanDirectory(artifacts);
		DotNetClean(solution, new DotNetCleanSettings { Configuration = configuration });
	});

Task("Restore")
	.Does(() => DotNetRestore(solution));

Task("Version")
	.Does(() =>
	{
		version = RunGitVersion();
		Information("Version: {0} ({1}, {2})", version.FullSemVer, version.ShortSha, version.CommitDate);
	});

Task("Build")
	.IsDependentOn("Restore")
	.IsDependentOn("Version")
	.Does(() => DotNetBuild(solution, new DotNetBuildSettings
	{
		Configuration = configuration,
		NoRestore = true,
		MSBuildSettings = VersionProperties(),
	}));

Task("Test")
	.IsDependentOn("Build")
	.Does(() => DotNetTest(testProject, new DotNetTestSettings
	{
		Configuration = configuration,
		NoRestore = true,
		NoBuild = true,
	}));

Task("Pack")
	.IsDependentOn("Test")
	.Does(() =>
	{
		CleanDirectory(artifacts);
		DotNetPack(libraryProject, new DotNetPackSettings
		{
			Configuration = configuration,
			OutputDirectory = artifacts,
			NoRestore = true,
			NoBuild = true,
			MSBuildSettings = VersionProperties(),
		});
	});

// Publishes to nuget.org. Needs NUGET_API_KEY; --skip-duplicate makes re-runs on the same tag idempotent.
Task("NuGetPush")
	.IsDependentOn("Pack")
	.Does(() =>
	{
		var apiKey = EnvironmentVariable("NUGET_API_KEY");
		if (string.IsNullOrWhiteSpace(apiKey))
			throw new CakeException("NUGET_API_KEY environment variable is not set.");

		foreach (var package in GetFiles($"{artifacts}/*.nupkg"))
		{
			Run("dotnet", $"nuget push \"{package.FullPath}\" --source https://api.nuget.org/v3/index.json --api-key {apiKey} --skip-duplicate");
			Information("Pushed {0}", package.GetFilename());
		}
	});

Task("Default")
	.IsDependentOn("Pack");

RunTarget(target);

sealed record GitVersionInfo(string FullSemVer, string SemVer, string ShortSha, string CommitDate);
