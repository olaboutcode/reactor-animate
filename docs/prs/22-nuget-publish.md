# PR 22 — Publish Reactor.Animate to NuGet

## Goal

The library is packable (`IsPackable`, `0.1.0-alpha`) but not published. Add a workflow that packs on tag (or release) and pushes to nuget.org.

## Files

- `.github/workflows/release.yml` — `on: push tags: ['v*']`
- `src/MauiAnimate/MauiAnimate.csproj` — `PackageProjectUrl`, `RepositoryUrl`, `PackageReadmeFile` if missing
- README install snippet: `dotnet add package Reactor.Animate --prerelease`

## Behavior

- `dotnet pack src/MauiAnimate/MauiAnimate.csproj -c Release` on macOS (iOS + Mac Catalyst TFMs)
- Trusted Publishing (OIDC) via `NuGet/login@v1`; no long-lived API key
- Do not pack samples

## Merge gate

`dotnet pack` succeeds locally. Workflow file is valid YAML. First publish is a manual tag `v0.1.0-alpha.1` after merge.

## Depends on

None.
