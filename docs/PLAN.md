# Reactor.Animate plan

Animation tools for MauiReactor. Not a Shell replacement and not a FluidNav port.

The public package and namespace is `Reactor.Animate`. The MAUI class library project is `MauiAnimate`.

## Goal

A reusable motion layer that can:

1. **Shared-element transitions** — tagged views on two pages; FLIP interpolates the frame (position, size, optional corner radius). Dest chrome can use `Animate.Motion` or MauiReactor `WithAnimation`.
2. **In-page motion (`Animate.Motion`)** — reusable recipes on any view: play / reverse / pause / reset / seek, fade / slide / scale, stagger, keyframes, timelines, repeat / yoyo, springs, path.
3. **Expand element → page** — dest grows from a tapped rect. Not shipped; GitHub PR #4.

Hero flights and in-page motion compose (hero + dest chrome via `BindMotion`). Whole-page fade/slide/scale is **not** on `Animate.Page`; those recipes live on `Animate.Motion`.

Later: interactive swipe can drive hero `t` (GitHub PR #5).

## What this is not

| Out of scope | Why |
|---|---|
| FluidNav `RouteMap` / URI router / singleton pages | MauiReactor `Navigation.PushAsync` already owns the stack |
| `IFluidHost` as the app shell | We wrap `NavigationPage`; we do not replace it |
| `FluidView` / `FluidPage` base class | Composition, not inheritance |
| Breakpoint system | Unrelated to motion |
| CollectionView insert/remove item anims | Separate from heroes |

FluidNav’s *motion* (hero, page expand, property tweens) is the inspiration. FluidNav the *product* (custom host + router) is not.

## Layers

```
Shipped                                              Later
───────────────────────────────────────────────      ────────────────
Animate.Page  shared-element FLIP                    Expand-to-page
Animate.Motion  play / reverse / pause / reset       Interactive pop
  keyframes, stagger, Add/Then, Repeat/Yoyo
  Seek / SeekFraction, springs, Path
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
- `Hero` — shared-element `Transition`
- `AnimatedHost` — wraps `NavigationPage`

## Navigation notes

- Use **MauiReactor `NavigationPage`**, not Shell, for morphing pairs.
- Intercept hardware back: reverse the clip, then pop with `animated: false`.
- Hide the system nav bar on morphing pages.
- iOS interactive swipe-back fights a custom morph until GitHub PR #5.
- Measure the source **at tap time**. Do not hold CollectionView cell references.

## Implementation order

1. Repo/solution scaffold — done.
2. `Animate.Motion` playable clips — done (design PRs 1–8).
3. Shared-element `Animate.Page` — done.
4. Motion follow-ups (repeat, exclusive bind, eased seek, timeline ids, HSV, FadeChrome opt-out, FlipClip clock, scrub, springs, path) — done (design PRs 9–16c).
5. Remaining GitHub drafts: expand-to-page (#4), interactive pop (#5), Image sample (#6), Path arcs (#7), CI platforms (#8), NuGet (#9), this PLAN refresh (#10).

## Repo layout

```
ReactorAnimate.slnx
Directory.Build.props
Directory.Packages.props
src/MauiAnimate
samples/Sample
tests/MauiAnimate.Tests
docs/PLAN.md
.github/workflows/ci.yml
```
