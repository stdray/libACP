param(
	[string]$Target = "Default",
	[string]$Configuration = "Release"
)

Set-Location $PSScriptRoot

dotnet tool restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet run build.cs --target=$Target --configuration=$Configuration @args
exit $LASTEXITCODE
