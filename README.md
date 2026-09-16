# Reactor.Animate

Shared-element page transitions for [MauiReactor](https://github.com/adospace/reactorui-maui).

Tag matching views on two pages. Push and pop play a shared-element clip instead of the platform slide. In-page motion stays with MauiReactor `WithAnimation`.

| | |
|:---|:---|
| Package | `Reactor.Animate` `0.1.0-alpha` |
| Namespace | `Reactor.Animate` |
| Targets | .NET 10 · MAUI 10 · Android · iOS · Mac Catalyst |
| License | MIT |

This is not a Shell replacement, not a router, and not a port of FluidNav. MauiReactor still owns the navigation stack.

---

## Quick start

Wrap the root page, tag matching views, and push through `Animate.Page`.

```csharp
class App : Component
{
    public override VisualNode Render()
        => new HomePage().AnimateHost();
}

class HomePage : Component
{
    public override VisualNode Render()
        => ContentPage(
            BoxView()
                .HeightRequest(72)
                .CornerRadius(12)
                .BackgroundColor(Colors.OrangeRed)
                .Hero("cover")
                .OnTapped(Open)
        )
        .HasNavigationBar(false);

    static Task Open()
        => Animate.Page.PushAsync<DetailPage>(t => t
            .Hero("cover", h => h.AnchorCenter())
            .WithDuration(400)
            .WithEasing(Easing.CubicOut));
}

class DetailPage : Component
{
    public override VisualNode Render()
        => ContentPage(
            VStack(
                BoxView()
                    .HeightRequest(180)
                    .CornerRadius(24)
                    .BackgroundColor(Colors.OrangeRed)
                    .Hero("cover"),
                Button("Back", async () => await Animate.Page.PopAsync())
            )
        )
        .HasNavigationBar(false);
}
```

`PopAsync` is a reverse hero on the source page: hold dest, pop, invert source heroes to dest frames with **negated extras**, then play to rest. `Rotate(90)` on push becomes `-90` on pop; `Translate(50, 0)` becomes `(-50, 0)`. The host intercepts Android system back and disables the iOS edge-swipe so a platform pop cannot run over the clip.

---

## How a flight works

```
push                                         pop
snapshot source                              snapshot dest heroes
hold (source)                                hold (dest)
push dest                                    pop dest
invert dest → source frames                  invert source → dest frames
  + extras (Rotate, Translate)                 + negated extras
HeroInFlight, hold lifts                     HeroInFlight, hold lifts
play dest to rest                            play source to rest
```

FLIP: the incoming view is laid out at rest, then translated and scaled to cover the outgoing frame. The clip plays those transforms back to rest. Push extras (`Rotate`, `Translate`) are negated on pop. Compatible bindable properties (color, corner radius) interpolate with the motion.

Non-hero content on the incoming page fades in after the shared-element clip is more than halfway through.

Defaults: **400 ms**, **`Easing.CubicOut`**.

---

## API

Everything lives in `Reactor.Animate`.

### Host

```csharp
new HomePage().AnimateHost();
```

| Member | Role |
|---|---|
| `VisualNode.AnimateHost()` | Wraps the tree in `AnimatedHost`. Prefer this over constructing the host directly. |
| `AnimatedHost` | Renders a `NavigationPage`, suppresses platform transitions, and wires back handling. |

Call this once at the app root. `Animate.Page` throws if the host is missing.

### Tags

```csharp
BoxView().Hero("cover")
Button("Open", Open).Hero("button")
```

| Member | Role |
|---|---|
| `VisualNode.Hero(string tag)` | Marks a view as a shared element. The tag must match on source and destination. |

Tags are ordinal strings. A view may have one tag. Several pairs may fly in the same push, one pair per shared tag.

### Navigation

```csharp
Animate.Page.PushAsync<DetailPage>();
Animate.Page.PushAsync<DetailPage>(t => t.Hero("cover"));
Animate.Page.PushAsync<DetailPage, DetailProps>(
    t => t.Hero("cover"),
    props => props.Id = id);
await Animate.Page.PopAsync();
```

| Member | Role |
|---|---|
| `PushAsync<TPage>(transitionFactory?)` | Pushes `TPage` with no platform animation and plays the transition. Omit the factory when there is no shared element. |
| `PushAsync<TPage, TProps>(transitionFactory, props)` | Same, with MauiReactor props. |
| `PopAsync()` | Reverse hero on source: invert to dest frames with negated extras, play to rest. No-ops when the stack has one page or when a flight is already running. |

Use these instead of `Navigation.PushAsync` / `PopAsync` for pages that participate in a flight.

### Transition

`Transition` is immutable. Methods return a new instance. Combine recipes with `|` or by chaining them; later values win for duration and easing. Hero layers append, so each tag can keep its own anchor and rotation.

```csharp
t => t
    .Hero("cover", h => h.AnchorCenter())
    .Hero("from_tl", h => h.AnchorTopLeft())
    .Hero("spin", h => h.AnchorCenter().Rotate(90))
    .WithDuration(300)
    .WithEasing(Easing.CubicInOut)
```

| Member | Role |
|---|---|
| `Transition.None` | Empty transition. The factory receives this. |
| `.Hero(params string[] tags)` | Shared-element flight for those tags. Shared extras apply to every tag in the call. |
| `.Hero(string tag, Func<Transition, Transition> configure)` | One tag with its own extras. |
| `.Anchor(x, y)` | Scale and rotation origin. `(0, 0)` is top-left, `(0.5, 0.5)` is center. |
| `.AnchorCenter()` `.AnchorTopLeft()` `.AnchorTopRight()` `.AnchorBottomLeft()` `.AnchorBottomRight()` | Named origins. |
| `.Rotate(degrees)` | Adds rotation to the invert, then plays back to rest. Pop uses `-degrees`. |
| `.Translate(x, y)` | Extra translation on the invert, in device-independent pixels. Pop uses `(-x, -y)`. |
| `.WithDuration(uint milliseconds)` | Clip length. Default `400`. |
| `.WithEasing(Easing)` | Clip easing. Default `Easing.CubicOut`. |
| `left \| right` | Merge. |

### Events

Subscribe once for the lifetime of the component, not inside a tap handler.

```csharp
protected override void OnMounted()
{
    Animate.Page.HeroStarted += OnHeroStarted;
    Animate.Page.HeroInFlight += OnHeroInFlight;
    Animate.Page.HeroEnded += OnHeroEnded;
    base.OnMounted();
}

protected override void OnWillUnmount()
{
    Animate.Page.HeroStarted -= OnHeroStarted;
    Animate.Page.HeroInFlight -= OnHeroInFlight;
    Animate.Page.HeroEnded -= OnHeroEnded;
    base.OnWillUnmount();
}

void OnHeroEnded(object? sender, HeroTransitionEventArgs e)
{
    if (e.Kind != HeroTransitionKind.Push)
        return;
    if (!e.Tags.Contains("cover"))
        return;
    // follow-up work on the live page
}
```

| Event | When | Safe to do |
|---|---|---|
| `HeroStarted` | `e.Page` is laid out. The hold still covers the window. Invert has not run. | Change page content, including hero layout. Changes are measured, then invert runs. |
| `HeroInFlight` | Hold is gone. The clip is visible. Raised once per flight, not per frame. | Work that should appear *with* the morph. Do not rely on changing flying-hero layout; it is locked. |
| `HeroEnded` | The clip has finished, or there was nothing to play. Always raised after `HeroStarted`, even if a handler throws. | Follow-up work on the live page. Layout is unlocked. |

`HeroTransitionEventArgs`

| Property | Meaning |
|---|---|
| `Kind` | `HeroTransitionKind.Push` or `Pop`. |
| `Page` | Destination on push; the page being revealed on pop. |
| `Transition` | The transition that was played. |
| `Tags` | Tags on that transition. |

The same handler instance is stored only once. A **new lambda on every tap** still stacks, because those are different delegates. Subscribe in `OnMounted` and unsubscribe in `OnWillUnmount`.

A throwing handler is logged and skipped. The flight still runs, and `HeroEnded` is still raised.

---

## Platform

| Platform | What the host does |
|---|---|
| iOS, Mac Catalyst | Disables the interactive edge-swipe on `UINavigationController`. That gesture would pop with the platform slide during the drag. |
| Android | Intercepts system back and predictive back, then calls `Animate.Page.PopAsync`. On the root page, back still leaves the app. |

Hide the navigation bar on pages that fly (`HasNavigationBar(false)`). The host already handles back navigation.

---

## Recipes

**Several heroes, per-tag extras**

```csharp
Animate.Page.PushAsync<DetailPage>(t => t
    .Hero("cover", h => h.AnchorCenter())
    .Hero("from_tl", h => h.AnchorTopLeft())
    .Hero("spin_90", h => h.AnchorCenter().Rotate(90))
    .WithEasing(Easing.CubicInOut));
```

**One cell from a grid**

```csharp
Animate.Page.PushAsync<GalleryDetailPage, GalleryItemProps>(
    t => t.Hero($"tile-{item.Id}", h => h.AnchorCenter()).WithDuration(300),
    props =>
    {
        props.Id = item.Id;
        props.Color = item.Color;
    });
```

The destination uses the same tag: `.Hero($"tile-{Props.Id}")`.

**Push with no shared element**

```csharp
await Animate.Page.PushAsync<GalleryPage>();
```

The hold still covers the page change. There is no morph.

---

## Guarantees

From `HeroInFlight` through `HeroEnded`, the library:

- Reverts size, margin, alignment, and visibility changes on **flying** heroes.
- Defers unregistration of those heroes until the clip ends, so a re-render cannot drop them mid-flight.
- Keeps writing the transforms the clip owns, so setting `Translation` or `Scale` in a handler does not stick.

Non-hero chrome is not locked. `HeroEnded` runs after the lock is released.

Busy flights and a one-page stack do not raise events.

---

## Status

This release covers shared-element push and pop. Expand-to-page and whole-page recipes (fade, slide, scale) are planned. A later release will treat every transition as a playable clip, with reverse and interactive `t`.
