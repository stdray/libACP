# Publishing to NuGet

This project publishes the `LibAcp` package to [nuget.org](https://www.nuget.org/packages/LibAcp).

## One-time setup

1. Create an API key on nuget.org:
   - Go to <https://www.nuget.org/account/apikeys> → **Create**.
   - Glob pattern: `LibAcp` (or `LibAcp*` if you plan to add packages later).
   - Scope: **Push new packages and package versions**.
   - Save the key value somewhere safe — nuget.org only shows it once.

2. Add the key as a GitHub Actions secret on the repo:
   - Repo → **Settings → Secrets and variables → Actions → New repository secret**.
   - Name: `NUGET_API_KEY`.
   - Value: the API key from step 1.

## Cutting a release

Releases are driven by **git tags**. The
[`release.yml`](.github/workflows/release.yml) workflow triggers on any tag
matching `v<major>.<minor>.<patch>` (optionally with a `-preview.N` suffix).

```pwsh
# 1. Bump the version in CHANGELOG.md and stamp the release date.
# 2. Commit and push that change.
git add CHANGELOG.md
git commit -m "release: 0.1.1"
git push

# 3. Tag and push the tag.
git tag v0.1.1
git push origin v0.1.1
```

The workflow will:

1. Restore, build (Release) and test.
2. `dotnet pack` the library and produce `LibAcp.X.Y.Z.nupkg` + `LibAcp.X.Y.Z.snupkg`.
3. Upload the `.nupkg`/`.snupkg` as workflow artifacts.
4. Push to nuget.org with `--skip-duplicate` (idempotent on retry).
5. Create a GitHub Release with auto-generated notes and attach the packages.

> The version passed to `dotnet pack` is derived from the tag, not from
> `<Version>` in the csproj. The csproj value is just the local-dev default.

## Pre-releases

Use a SemVer pre-release suffix in the tag:

```pwsh
git tag v0.2.0-preview.1
git push origin v0.2.0-preview.1
```

nuget.org will mark the package as a pre-release and hide it from default
search results.

## Local dry-run

Before tagging, you can verify the package builds and inspect its contents:

```pwsh
dotnet pack src/Acp/Acp.csproj -c Release -o artifacts -p:Version=0.1.0
# Inspect:
tar -tvf artifacts/LibAcp.0.1.0.nupkg            # on linux/mac
# or open artifacts/LibAcp.0.1.0.nupkg in any zip viewer on Windows
```

## Pulling a release

If you ever need to unlist a broken version (you cannot delete from nuget.org):

```pwsh
dotnet nuget delete LibAcp 0.1.0 --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
```

`delete` on nuget.org actually unlists; the package remains downloadable for
existing consumers but won't appear in new searches.
