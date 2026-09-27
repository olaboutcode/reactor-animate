# PR 21 — CI: build Android (and macOS iOS) as well as net10.0 tests

## Goal

`.github/workflows/ci.yml` only builds `net10.0` and runs unit tests on Ubuntu. Add a job that builds the library (and optionally the sample) for `net10.0-android`. Optionally a `macos-latest` job for `net10.0-ios` **build** (no simulator run).

## Files

- `.github/workflows/ci.yml`

## Jobs

1. Keep **ubuntu**: restore MAUI, `dotnet test` (current).
2. **ubuntu** or **windows**: `dotnet build src/MauiAnimate/MauiAnimate.csproj -f net10.0-android -c Release` (needs Android workload + JDK).
3. Optional **macos-latest**: `dotnet build -f net10.0-ios` (no `dotnet test` on device).

Do not block PRs on a full simulator boot.

## Merge gate

CI green on a dummy push. net10.0 test job still the required check.

## Depends on

Current `ci.yml`.
