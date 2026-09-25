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
# 1. Turn [Unreleased] in CHANGELOG.md into the new version + date, commit, push to main.
git commit -am "release: 0.2.0"
git push

# 2. Tag the commit on main and push the tag.
git tag v0.2.0
git push origin v0.2.0
```

The workflow runs `./build.sh --target=NuGetPush`, which:

1. Computes the version with GitVersion — on a tagged commit that is exactly the tag (`0.2.0`).
2. Restores, builds (Release) and tests.
3. Packs `LibAcp.X.Y.Z.nupkg` + `.snupkg` into `./artifacts`.
4. Pushes to nuget.org with `--skip-duplicate` (idempotent on retry).

It then uploads the packages as workflow artifacts and creates a GitHub Release.

> Versions come from GitVersion (`GitVersion.yml`), never from the csproj. Untagged commits get
> pre-release versions: `<next>-ci.N` on `main`, `<next>-<branch>.N` on feature branches, where `<next>` is the last tag + patch.
> `next-version` in `GitVersion.yml` sets the floor when no tag exists yet.

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
./build.ps1 -Target Pack     # version from GitVersion, packages in ./artifacts
dotnet tool run dotnet-gitversion . /showvariable FullSemVer   # just print the version
```

## Pulling a release

If you ever need to unlist a broken version (you cannot delete from nuget.org):

```pwsh
dotnet nuget delete LibAcp 0.1.0 --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
```

`delete` on nuget.org actually unlists; the package remains downloadable for
existing consumers but won't appear in new searches.
