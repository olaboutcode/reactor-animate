# PR 20 — Sample ArcSegment on Motion.Path

## Goal

`PathSampler` currently skips `ArcSegment`. Flatten arcs (SVG elliptical-arc → cubics or a 12-step polyline) so `.Path(geometry)` follows arcs.

## Files

- `src/MauiAnimate/Motion/PathSampler.cs` — `case ArcSegment:`
- `tests/MauiAnimate.Tests/MotionPathTests.cs` — 90° quarter-circle: at `t=0.5` point is near the arc midpoint, not the chord
- Optional: Path sample adds an arc figure

## Merge gate

Existing line/Bézier tests still pass. New arc test green.

## Depends on

PR 16c (`PathSampler`).
