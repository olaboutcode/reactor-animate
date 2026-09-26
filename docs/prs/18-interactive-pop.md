# PR 18 — Drive the hero clip from system back gestures

## Goal

iOS edge-swipe and Android predictive back scrub the **hero** clip (`t` 0→1) instead of the platform slide. Finger-up commits (`t→1` pop) or cancels (`t→0`).

Motion scrub (`SeekFraction`) is already shipped. This PR is **Page only**.

## Public API

No new push factory required. Host already intercepts back. Replace “gesture → PopAsync” with “gesture → seek FlipClip, then finish or cancel”.

## Files

- `src/MauiAnimate/Page/AnimatedHost.cs` / platform handlers — feed `t` from the gesture
- `src/MauiAnimate/Animation/FlipTween.cs` — pause/seek/resume the in-flight clip (linear `u`, same clock as PR 15)
- `src/MauiAnimate/Page/Nav.cs` — commit vs cancel pop after gesture ends
- iOS: keep edge-swipe **disabled** until this PR lands a working scrub; then re-enable a controlled pan
- Android: predictive back progress → `Seek`

## Merge gate

Device: swipe back on Gallery detail and Circle; cancel mid-swipe restores dest; complete swipe pops with reverse hero. Motion scrub sample unchanged.

## Depends on

PR 15 (`FlipClip` on `MotionClock`). Do not mix with Expand.
