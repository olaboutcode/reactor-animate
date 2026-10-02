# Reactor.Animate plan

Animation tools for MauiReactor. Not a Shell replacement and not a FluidNav port.

The public package and namespace is `Reactor.Animate`. The MAUI class library project is `MauiAnimate`.

## Goal

A reusable motion layer that can:

1. **Shared-element transitions** — tagged views on two pages; FLIP interpolates the frame (position, size, optional corner radius). Dest chrome can use `Animate.Motion` or MauiReactor `WithAnimation`.
2. **In-page motion (`Animate.Motion`)** — reusable recipes on any view: play / reverse / pause / reset / seek, fade / slide / scale, stagger, keyframes, timelines, repeat / yoyo, springs, path.

Hero flights and in-page motion compose (hero + dest chrome via `BindMotion`). Whole-page fade/slide/scale is **not** on `Animate.Page`; those recipes live on `Animate.Motion`.

Expand-to-page and interactive pop are out of scope (GitHub #4 and #5 closed).

## What this is not

| Out of scope | Why |
|---|---|
| FluidNav `RouteMap` / URI router / singleton pages | MauiReactor `Navigation.PushAsync` already owns the stack |
| `IFluidHost` as the app shell | We wrap `NavigationPage`; we do not replace it |
| `FluidView` / `FluidPage` base class | Composition, not inheritance |
| Breakpoint system | Unrelated to motion |
| CollectionView insert/remove item anims | Separate from heroes |
| Expand-to-page | Closed GitHub #4 |
| Interactive pop (edge-swipe hero scrub) | Closed GitHub #5 |

FluidNav’s *motion* (hero, page expand, property tweens) is the inspiration. FluidNav the *product* (custom host + router) is not.

## Layers

```
Shipped
───────────────────────────────────────────────
Animate.Page  shared-element FLIP
Animate.Motion  play / reverse / pause / reset
  keyframes, stagger, Add/Then, Repeat/Yoyo
  Seek / SeekFraction, springs, Path (line, Bézier, arc)
  HSV color, exclusive bind, WithoutChromeFade
```

- **Motion** works without navigation (`Motion.Bind` / `BindMotion`).
- **Page** recipes are shared-element only. Fade/slide/scale are `Animate.Motion`.
- MauiReactor `WithAnimation()` still owns **state-flag** layout morphs.
- Reactor.Animate owns **cross-page** heroes and **reusable in-page** clips.

## Architecture

```
new HomePage().AnimateHost()   // once, around NavigationPage
  ├── NavigationPage
  │     ├── ListPage
  │     │     Image(...).Hero("cover")
  │     └── DetailPage
  │           Image(...).Hero("cover")
  └── Overlay / hold frame     // in-flight
```

1. **`.AnimateHost()` / `.Host()`** — wraps `NavigationPage`, suppresses the platform slide.
2. **`.Hero(tag)`** — registers `{ tag, nativeView }` with the host.
3. **`Animate.Page.PushAsync` / `PopAsync`** — capture source bounds, push without platform animation, play the clip.

Pages stay ordinary `Component`s. Matching is by **tag**.

## Recipes

| Recipe | What moves |
|---|---|
| Shared element (`Animate.Page`) | Tagged views |
| In-page fade / slide / scale (`Animate.Motion`) | Bound views, not the whole page |

| Push | Result |
|---|---|
| Hero only | Image flies; page underneath is already there |
| Hero + Motion chrome | Image flies; dest chrome plays via `BindMotion` after `HeroInFlight` |

The library animates the **frame**; `WithAnimation` / `MotionPlayer` animate the **insides**.

## Suggested API

See README. Types:

- `Animate` — static facade
- `Animate.Page` — push, pop
- `Animate.Motion` — in-page recipes and playback
- `Transition` — `.Hero`, `.Rotate`, `.Anchor`, `.WithDuration`, `.WithEasing`, `.WithoutChromeFade`
- `Motion` / `MotionPlayer` — recipe and playback
- `HeroTransition` — shared-element `Transition`
- `AnimatedHost` — wraps `NavigationPage`

## Navigation notes

- Use **MauiReactor `NavigationPage`**, not Shell, for morphing pairs.
- Intercept hardware back: reverse the clip, then pop with `animated: false`.
- Hide the system nav bar on morphing pages.
- iOS interactive swipe-back is disabled so a platform pop cannot run over the clip.
- Measure the source **at tap time**. Do not hold CollectionView cell references.

## Implementation order

1. Repo/solution scaffold — done.
2. `Animate.Motion` playable clips — done (design PRs 1–8).
3. Shared-element `Animate.Page` — done.
4. Motion follow-ups (repeat, exclusive bind, eased seek, timeline ids, HSV, FadeChrome opt-out, FlipClip clock, scrub, springs, path) — done (design PRs 9–16c).
5. Image-to-Image sample (#6), Path arcs (#7), CI Android/iOS (#8), PLAN refresh (#10) — merged.
6. Expand-to-page (#4) and interactive pop (#5) — closed, out of scope.
7. Remaining GitHub draft: NuGet publish (#9).

## Repo layout

```
ReactorAnimate.slnx
Directory.Build.props
Directory.Packages.props
src/MauiAnimate
samples/Sample
tests/MauiAnimate.Tests
docs/                 # library docs (MkDocs). PLAN.md is personal; excluded from the site.
mkdocs.yml
.github/workflows/ci.yml
.github/workflows/docs.yml
```
