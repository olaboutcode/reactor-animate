# Reactor.Animate plan

Animation tools for MauiReactor. Not a Shell replacement and not a FluidNav port.

The public package and namespace is `Reactor.Animate`. The MAUI class library project is `MauiAnimate`.

## Goal

A reusable motion layer that can:

1. **Shared-element transitions** — tagged views on two pages; FLIP interpolates the frame (position, size, optional corner radius). The child can still use MauiReactor `WithAnimation` or `Animate.Motion` for in-page chrome.
2. **In-page motion (`Animate.Motion`)** — reusable recipes bound to any view: play / reverse / pause / reset, fade / slide / scale, stagger, keyframes, timelines.
3. **Expand element → page** — later. Not a v1 `Animate.Page` recipe.

Hero flights and in-page motion compose (hero + dest chrome via `BindMotion`). Whole-page fade/slide/scale is **not** on `Animate.Page`; those recipes live on `Animate.Motion`.

Later: every transition is a **playable clip**. Push is `PlayAsync()` (0→1). Pop is `ReverseAsync()` (1→0). Interactive swipe can drive `t`.

## What this is not

| Out of scope | Why |
|---|---|
| FluidNav `RouteMap` / URI router / singleton pages | MauiReactor `Navigation.PushAsync` already owns the stack |
| `IFluidHost` as the app shell | We wrap `NavigationPage`; we do not replace it |
| `FluidView` / `FluidPage` base class | Composition, not inheritance |
| Port of `Flows()` / `On<TView>()` | In-page tweens are `Animate.Motion` and MauiReactor `WithAnimation` |
| Breakpoint system | Unrelated to motion |
| Springs, CollectionView item anims, path motion | Not v1 |

FluidNav’s *motion* (hero, page expand, fade, property tweens) is the inspiration. FluidNav the *product* (custom host + router) is not.

## Layers

```
Tools                         Recipes (v1)                    Later
─────────────────────────     ──────────────────────────      ─────────────
Animate.Motion (clip)         1. Shared element (Page)        Expand-to-page
  play / reverse / pause      2. In-page fade/slide/scale     Interactive (drag)
  keyframes, stagger, Add/Then                                Springs, paths
```

- **Tools** work without navigation (`Motion.Bind` / `BindMotion`).
- **Page recipes** are shared-element only. Fade/slide/scale are `Animate.Motion`.
- MauiReactor `WithAnimation()` still owns **state-flag** layout morphs (compact card → expanded header).
- Reactor.Animate owns **cross-page** heroes and **reusable in-page** clips.

## Architecture

```
new HomePage().Host()              // once, around NavigationPage
  ├── NavigationPage
  │     ├── ListPage
  │     │     Image(...).Hero("cover")
  │     └── DetailPage
  │           Image(...).Hero("cover")
  └── Overlay (AbsoluteLayout)     // in-flight visuals
```

Three pieces:

1. **`.Host()`** — wraps `NavigationPage`, suppresses the platform slide, owns the overlay, is the only place that should `PushAsync`/`PopAsync` with `animated: false`.
2. **`.Hero(tag)`** — extension on any visual node. Registers `{ tag, nativeView }` with the host.
3. **`Animate.Page.PushAsync` / `PopAsync`** — capture source bounds, push without platform animation, wait for dest layout, play the clip.

Pages stay ordinary `Component`s. Opt in by wrapping widgets and calling `Animate.Page.PushAsync`. No page base class. Other animation families attach as siblings (`Animate.Motion`, and so on).

Matching is by **tag**, like Flutter `Hero` / Android `transitionName`. Source and dest do not need the same component type.

## v1 recipes

| Recipe | What moves |
|---|---|
| Shared element (`Animate.Page`) | Only tagged views |
| In-page fade / slide / scale (`Animate.Motion`) | Bound views, not the whole page |

Combinations:

| Push | Result |
|---|---|
| Hero only | Image flies; page underneath is already there |
| Hero + Motion chrome | Image flies; dest chrome plays via `BindMotion` after `HeroInFlight` |

Internal morph is **author-composed**: the `Hero` child is a dual-layout component (`Phase.Compact` / `Phase.Expanded`) with `WithAnimation`. The library animates the **frame**; the component animates the **insides**.

Do not start with FluidNav’s dual-layout `On<List>() / On<Detail>()` flows. Default shared-element motion is the frame. Dual-layout components are opt-in on that component.

## Suggested API

```csharp
using Reactor.Animate;

class App : Component
{
    public override VisualNode Render()
        => new HomePage().Host();
}

class HomePage : Component
{
    public override VisualNode Render()
        => ContentPage(
            Image(item.Banner).Hero("cover")
        );

    Task Open()
        => Animate.Page.PushAsync<DetailPage, DetailProps>(t => t.Hero("cover"), p => p.Id = id);
}

class DetailPage : Component
{
    public override VisualNode Render()
        => ContentPage(
            Image(item.Banner).Hero("cover"),
            Button("Back", async () => await Animate.Page.PopAsync())
        );
}
```

`Animate` is a static class. Page navigation is `Animate.Page.*`. Transitions are built with factory methods on `Transition`. Per-item motion: `.Hero("a", h => h.AnchorCenter())`. Shared motion: `.Hero("c", "d").AnchorCenter()`. `.Hero` FLIPs position, scale, and shared interpolatable properties (color, font size, corner radius). `.Rotate(degrees)` and `.Anchor` are the remaining invert knobs. VisualNode extensions `.Hero(tag)` and `.Host()` tag and host the tree.

Types:

- `Animate` — static facade
- `Animate.Page` — push, pop
- `Animate.Motion` — in-page recipes and playback
- `Transition` — abstract page transition (`.Hero`, `.Rotate`, `.Anchor`, `.WithDuration`, `.WithEasing`)
- `Motion` — immutable in-page recipe
- `MotionPlayer` — play / reverse / pause / reset
- `Hero` — shared-element `Transition`
- `AnimatedHost` — wraps `NavigationPage` (created via `.Host()` / `.AnimateHost()`)

Clips store from/to. v1 can pop by reversing the last push even if a general `Reverse()` API ships later. Do not implement pop as a different animation, and do not use fire-and-forget `FadeTo` as the core model.

## Navigation notes

- Use **MauiReactor `NavigationPage`**, not Shell, for morphing pairs. Shell owns presentation; you can only turn its animation on/off.
- `PushAsync<T, TProps>` may not expose `animated: false`. Use the native `INavigation.PushAsync(Page, bool)` if needed.
- Intercept hardware back: collapse/reverse first, then pop with `animated: false`.
- Hide the system nav bar on morphing pages (or both); a bar appearing mid-flight looks like a second transition.
- iOS interactive swipe-back fights a custom morph. Disable it on these pages in v1.
- Measure the source **at tap time** (`Rect` relative to the overlay). Do not hold CollectionView cell references.

## Implementation order

1. Repo/solution scaffold (this drop).
2. Tools: `Animate.Motion` playable clips (done on `the_flutter_way`, PRs 1–8).
3. Shared-element recipe on `Animate.Page.PushAsync`. Fade/slide/scale on `Animate.Motion`, not Page.
4. Samples: playlist-style expand + hero, and a fade-only page pair; plus `Image` → larger `Image` (hero only).
5. Reverse as clip playback, not a rewrite.

Post-v1 (see `docs/ANIMATE_MOTION.md` PRs 9–16): Repeat/Yoyo, exclusive player, eased Seek, timeline ids, HSV lerp, FadeChrome opt-out, FlipClip on MotionClock, interactive `t` / springs / path.

## Repo layout

```
ReactorAnimate.slnx
Directory.Build.props          // shared MSBuild + CPM enable
Directory.Packages.props       // package versions
src/MauiAnimate                // MAUI + MauiReactor class library (Reactor.Animate)
samples/Sample                 // MauiReactor startup app referencing the library
docs/PLAN.md                   // this file
```
