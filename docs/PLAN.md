# Reactor.Animate plan

Animation tools for MauiReactor. Not a Shell replacement and not a FluidNav port.

The public package and namespace is `Reactor.Animate`. The MAUI class library project is `MauiAnimate`.

## Goal

A reusable motion layer that can:

1. **Shared-element transitions** — tagged views on two pages; an overlay interpolates the frame (position, size, optional corner radius). The child can still use MauiReactor `WithAnimation` for internal layout morphs.
2. **Expand element → page** — the destination surface starts as the source element’s rect and grows to fill the page.
3. **Whole-page transitions** — the page moves as a unit (fade, slide, scale) with no shared-element origin.

These compose (hero + fade, expand + hero, page-only, and so on).

Later: every transition is a **playable clip**. Push is `PlayAsync()` (0→1). Pop is `ReverseAsync()` (1→0). Interactive swipe can drive `t`.

## What this is not

| Out of scope | Why |
|---|---|
| FluidNav `RouteMap` / URI router / singleton pages | MauiReactor `Navigation.PushAsync` already owns the stack |
| `IFluidHost` as the app shell | We wrap `NavigationPage`; we do not replace it |
| `FluidView` / `FluidPage` base class | Composition, not inheritance |
| Port of `Flows()` / `On<TView>()` | In-page tweens are MauiReactor `WithAnimation` |
| Breakpoint system | Unrelated to motion |
| Springs, CollectionView item anims, path motion | Not v1 |

FluidNav’s *motion* (hero, page expand, fade, property tweens) is the inspiration. FluidNav the *product* (custom host + router) is not.

## Layers

```
Tools                         Recipes (v1)                    Later
─────────────────────────     ──────────────────────────      ─────────────
tween (props → targets)       1. Shared element               Play / Reverse
timeline (parallel/sequence)  2. Expand element → page        Interactive (drag)
easing, duration              3. Whole-page transition        Springs, paths
overlay host
```

- **Tools** work without navigation (expand a panel, unit-test a clip).
- **Recipes** are thin wrappers that play clips on push/pop.
- MauiReactor `WithAnimation()` still owns **in-page** state morphs (compact card → expanded header).
- Reactor.Animate owns **cross-page** motion and reusable clips.

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
| Shared element | Only tagged views |
| Expand from element | Dest page/body grows from the tapped rect |
| Whole page | Entire page fades / slides / scales |

Combinations:

| Push | Result |
|---|---|
| Hero only | Image flies; page underneath is already there |
| Expand only | Page grows out of the tapped card |
| Page only | Fade/slide/scale between pages |
| Hero + fade | Image flies; the rest of the dest fades in |
| Expand + hero | Card grows into the page and internals can morph |

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
- `Transition` — abstract page transition (`.Hero`, `.Rotate`, `.Anchor`, `.WithDuration`, `.WithEasing`)
- `Hero` — shared-element `Transition`
- `AnimatedHost` — wraps `NavigationPage` (created via `.Host()`)

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
2. Tools: tween + overlay host + playable clip.
3. Three recipes on `Animate.Page.PushAsync`.
4. Samples: playlist-style expand + hero, and a fade-only page pair; plus `Image` → larger `Image` (hero only).
5. Reverse as clip playback, not a rewrite.

## Repo layout

```
ReactorAnimate.slnx
Directory.Build.props          // shared MSBuild + CPM enable
Directory.Packages.props       // package versions
src/MauiAnimate                // MAUI + MauiReactor class library (Reactor.Animate)
samples/Sample                 // MauiReactor startup app referencing the library
docs/PLAN.md                   // this file
```
