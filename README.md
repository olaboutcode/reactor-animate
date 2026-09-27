# Reactor.Animate

Shared-element page transitions and in-page view motion for [MauiReactor](https://github.com/adospace/reactorui-maui).

Tag matching views on two pages. Push and pop play a shared-element clip instead of the platform slide. In-page motion uses `Animate.Motion` (reusable recipes, play / reverse / pause / reset) or MauiReactor `WithAnimation` (state morphs).

| | |
|:---|:---|
| Package | `Reactor.Animate` `0.1.0-alpha` |
| Namespace | `Reactor.Animate` · `Reactor.Animate.Animation` |
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

`Reactor.Animate.Animation.Transition` is immutable. Methods return a new instance. Combine recipes with `|` or by chaining them; later values win for duration and easing. Hero layers append, so each tag can keep its own anchor and rotation.

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
| `.WithoutChromeFade()` | Skip fading non-hero chrome on this flight. Dest Motion chrome can rest at `TranslationX` 0. |
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

void OnHeroInFlight(object? sender, HeroTransitionEventArgs e)
{
    if (e.Kind != HeroTransitionKind.Push)
        return;
    e.At(t =>
    {
        if (t >= 0.5)
            SetState(s => s.ShowChrome = true);
    });
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
| `HeroInFlight` | Hold is gone. The clip is visible. Raised once per flight, not per frame. | Work that should appear *with* the morph. Do not rely on changing flying-hero layout; it is locked. Follow the curve with `e.At(t => ...)`. |
| `HeroEnded` | The clip has finished, or there was nothing to play. Always raised after `HeroStarted`, even if a handler throws. | Follow-up work on the live page. Layout is unlocked. |

`HeroTransitionEventArgs`

| Property / method | Meaning |
|---|---|
| `Kind` | `HeroTransitionKind.Push` or `Pop`. |
| `Page` | Destination on push; the page being revealed on pop. |
| `Transition` | The transition that was played. |
| `Tags` | Tags on that transition. |
| `Progress` | 0–1 along the clip, using the same easing as the flight (`e.Transition.Easing`). 0 at `HeroStarted`, 1 at `HeroEnded`. |
| `At(callback)` | `Action<double>` invoked with `Progress` now and on each tick of this flight. |

The same handler instance is stored only once. A **new lambda on every tap** still stacks, because those are different delegates. Subscribe in `OnMounted` and unsubscribe in `OnWillUnmount`.

A throwing handler is logged and skipped. The flight still runs, and `HeroEnded` is still raised.

### Motion

`Reactor.Animate.Animation.Motion` is an immutable recipe with no targets. Bind it to any `VisualElement` (or `VisualNode` via `BindMotion`). Playback lives on `MotionPlayer`. Defaults: **300 ms**, **`Easing.CubicOut`**. A second bind on the same view disposes the previous player.

```csharp
static readonly Motion Pulse = Animate.Motion.Define(m => m
    .Scale(1, 1.08)
    .WithDuration(180));

Button("Pulse")
    .BindMotion(Pulse, p => _pulse = p)
    .OnTapped(async () =>
    {
        if (_pulse is null) return;
        if (_pulse.Status == MotionPlaybackStatus.Completed)
            _pulse.Reverse();
        else
            _pulse.Forward();
    });
```

Hero flights fade non-hero chrome by default. Skip that with `WithoutChromeFade()` so dest Motion chrome can rest at `TranslationX` 0. Views with `TranslationX` / `TranslationY` ≠ 0 are still skipped.

```csharp
await Animate.Page.PushAsync<Detail>(t => t.Hero("orb").WithoutChromeFade());

VStack(…)
    .Opacity(0)
    .BindMotion(
        Animate.Motion.Define(m => m.FadeIn().TranslateX(-100, 0).WithDuration(300)),
        p => _chrome = p);
```

Start chrome from `HeroInFlight` (`e.At(t => …)`), not `HeroEnded`, if it should overlap the flight.

| Member | Role |
|---|---|
| `Animate.Motion.Define` | Build a recipe from `Motion.None`. |
| `Animate.Motion.Bind` / `Motion.Bind` | Bind to one or more views. Does not start. |
| `Animate.Motion.Play` | Bind and start forward. Returns the player. |
| `Animate.Motion.ForwardAsync` / `ReverseAsync` | One-shot bind and play. |
| `VisualNode.BindMotion(motion, onBind?)` | Bind on Loaded, dispose on Unloaded. Subscribe in `onBind`. |
| `.Opacity` `.Translate` `.Scale` `.Rotate` `.BackgroundColor` `.Width` `.Height` `.CornerRadius` `.Property` `.Path` | Property tracks. `Scale` writes ScaleX and ScaleY. `Path` writes TranslationX/Y along a `PathGeometry`. Colors lerp in HSV (shortest hue); `.WithColorSpace(ColorSpace.Rgb)` for channel-wise. |
| `.FadeIn` `.FadeOut` `.SlideIn` `.SlideOut` `.ScaleIn` `.ScaleOut` | Named recipes. `SlideIn` / `SlideOut` take `SlideFrom`. |
| `.Stagger(step, from, grid?)` | Delay each bound target on linear player time (`StaggerFrom.Start` / `Center` / `End`). |
| `.Keyframes` / `.Opacity(k => k.At(…))` | 0–1 offsets of this motion. |
| `.Add(child, at, id?)` / `.Then(next, id?)` | Timeline. `Then` starts at the current span. `TrySpan` / `Seek(id)` use the name. |
| `.Repeat(n)` / `.Yoyo()` | `Repeat(1)` is once; `Repeat(-1)` until Pause, Reset, or Dispose. A yoyo cycle is forward then reverse. |
| `.WithSpring(Spring)` | Mass-spring-damper until rest (`Spring.Default` / `Snappy` / `Gentle`). Not an easing. Do not combine with `WithDuration` / `Stagger`. |
| `left \| right` | Parallel merge; parent span is `max(left, right)`. Tracks are not stretched. |
| `Forward()` / `Reverse()` | Start. Null-safe; does not throw on cancel. |
| `ForwardAsync()` / `ReverseAsync()` | Same, awaitable. Null-safe; does not throw on cancel. |
| `Seek(ms)` | Linear wall-clock position. Leaves `Paused` for a mid-span seek. |
| `SeekFraction(t)` | Eased progress (same units as `Progress` / `At`). Leaves `Paused`; does not play. |
| `Seek(id)` / `TrySpan(id, …)` | Named timeline child, linear player time. |
| `MotionPlayer.At` | Player-long progress ticks (eased `t`; decreases on reverse). Springs report the spring `x`. |
| `Started` `Completed` `Paused` `Resumed` `StatusChanged` | Lifecycle. |

`Animate.Motion.Play` is bind + start + **keep the player** (pause or reverse later). `Animate.Motion.ForwardAsync` also starts, but you only get a Task — no player. Use `Bind` / `BindMotion` when you will call `Forward()` yourself.

```csharp
Animate.Motion.Play(
    m => m.FadeIn().Scale(0.9, 1).Stagger(40, StaggerFrom.Start, (3, 4)).WithDuration(280),
    tiles);

.Add(fade, at: 0, id: "intro").Then(pulse, id: "pulse");
player.Seek("pulse");

.Scale(1, 1.16).WithSpring(Spring.Snappy);

.Path(geometry).WithDuration(900).WithEasing(Easing.SinInOut);
```

Use `WithAnimation` when a state flag should morph layout. Use `MotionPlayer` when you need reverse, pause, stagger, or a recipe reused on any view. Do not drive the same property with both.

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

**Stagger a grid**

```csharp
Animate.Motion.Play(
    m => m.FadeIn().Scale(0.9, 1).Stagger(40, StaggerFrom.Start, (3, 4)).WithDuration(280),
    tiles);
```

**Scrub**

`SeekFraction(t)` leaves the player paused. Lifting the slider does not resume; call `ForwardAsync` to play.

---

## Sample

The sample app (`samples/Sample`) is a MauiReactor gallery:

| Page | What it shows |
|---|---|
| Home heroes | Several tagged boxes, per-tag anchors and rotation |
| Gallery | CollectionView cell → detail hero |
| Circle to top | Orb hero plus dest chrome via `BindMotion` |
| Motion playground | Fade, slide, scale, pulse, yoyo, bounce, keyframes, intro, spring pop |
| Stagger grid | 12 tiles, grid stagger |
| Scrub | Slider → `SeekFraction` |
| Color HSV vs RGB | Same red→lime, two color spaces |
| Timeline seek | Named `Add` / `Then` + `Seek(id)` |
| Photos | Image cell → detail hero (same `Source`) |
| Path | Bézier and `ArcSegment` lanes |

---

## CI

GitHub Actions (`.github/workflows/ci.yml`) runs `MauiAnimate.Tests` on Ubuntu (`net10.0`) and builds the library for `net10.0-android` and `net10.0-ios`. No simulator or device run.

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

Shared-element push/pop and in-page `Animate.Motion` are on `main`: play / reverse / pause / reset / seek, keyframes, stagger, timelines, repeat / yoyo, springs, path (line, Bézier, arc), HSV color, exclusive bind, and `WithoutChromeFade`. Fade, slide, and scale recipes live on `Animate.Motion`, not on `Animate.Page`. Expand-to-page and interactive pop are out of scope. Design notes: `docs/ANIMATE_MOTION.md`. Living plan: `docs/PLAN.md`.
