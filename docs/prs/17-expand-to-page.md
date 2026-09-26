# PR 17 — Expand a tagged source frame into the destination page

## Goal

Dest page (or a tagged body) starts at the source element’s window rect and grows to fill the page. Compose with `.Hero(...)` so internals can still fly.

This is **not** a whole-page fade/slide recipe. Those live on `Animate.Motion`.

## Public API

```csharp
Animate.Page.PushAsync<Detail>(t => t
    .Expand("card")
    .Hero("cover", h => h.AnchorCenter())
    .WithDuration(400));
```

- `Expand(string tag)` — source of the start rect; dest uses the same tag or a page-level clip.
- Pop reverses: dest shrinks back to the source rect, then pop.
- Measure source **at tap time** (`Geometry.GetWindowBounds`). Do not hold CollectionView cell refs.

## Files

- `src/MauiAnimate/Animation/Transition.cs` — `Expand(string tag)`
- `src/MauiAnimate/Page/Hero.cs` or `Expand.cs` — layer + invert
- `src/MauiAnimate/Page/Nav.cs` — start rect → rest bounds clip (FLIP or clip-to-page)
- `samples/Sample` — playlist-style card → page (PLAN sample)

## Merge gate

Home-style grid + CollectionView: dest grows from the tapped cell, reverse shrinks. Hero-only pages unchanged. No new page-level Fade/Slide.

## Depends on

Current FlipClip / `Geometry.GetWindowBounds`. Independent of Motion.
