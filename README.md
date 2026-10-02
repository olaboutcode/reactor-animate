# Reactor.Animate

An animation library for [MauiReactor](https://github.com/adospace/reactorui-maui). It is inspired by MauiReactor's own property and animation controller, flutter, animejs, and pulls some elements from [beto-rodriguez](https://github.com/beto-rodriguez)'s [FluidNav](https://github.com/beto-rodriguez/FluidNav).

> [!WARNING]
> The library is still experimental, use at your own risk. The performance seems decent and all the features should be working as expected. However, expect breaking changes as I work on the API.

## Table of contents

- [Overview](#overview)
- [Getting started](#getting-started)
  - [Install](#install)
  - [Examples](#examples)
  - [Namespaces](#namespaces)
  - [Host](#host)
- [Page](#page)
  - [Push and pop](#push-and-pop)
  - [Heroes](#heroes)
  - [Transitions](#transitions)
    - [Duration and easing](#duration-and-easing)
    - [Anchors](#anchors)
    - [Rotate and translate](#rotate-and-translate)
    - [Several heroes](#several-heroes)
    - [Chrome fade](#chrome-fade)
    - [Merge](#merge)
  - [Events](#events)
  - [Progress](#progress)
  - [What a flight does](#what-a-flight-does)
  - [While a hero is in flight](#while-a-hero-is-in-flight)
  - [Platform](#platform)
- [Motion](#motion)
  - [Recipes](#recipes)
  - [Property tracks](#property-tracks)
    - [Opacity](#opacity)
    - [Translation](#translation)
    - [Scale](#scale)
    - [Rotation](#rotation)
    - [Depth](#depth)
    - [Color](#color)
    - [Size](#size)
    - [Corner radius](#corner-radius)
    - [Any property](#any-property)
  - [Named recipes](#named-recipes)
  - [Timing](#timing)
  - [Color space](#color-space)
  - [Bind and play](#bind-and-play)
  - [BindMotion](#bindmotion)
  - [MotionPlayer](#motionplayer)
    - [Forward and reverse](#forward-and-reverse)
    - [Pause resume and reset](#pause-resume-and-reset)
    - [Status](#status)
    - [Player events](#player-events)
  - [Seek](#seek)
  - [Repeat and yoyo](#repeat-and-yoyo)
  - [Parallel merge](#parallel-merge)
  - [Stagger](#stagger)
  - [Keyframes](#keyframes)
  - [Timeline](#timeline)
  - [Springs](#springs)
  - [Path](#path)
  - [One player per view](#one-player-per-view)
  - [Motion during a flight](#motion-during-a-flight)
- [Samples](#samples)
- [Building](#building)
- [License](#license)

## Overview

There are two families.

`Animate.Page` flies shared elements from one page to the next. Tag the views with `.Hero(tag)`, then push and pop through `Animate.Page`. The flight is a FLIP: the incoming view is laid out at rest, moved and scaled so it covers the outgoing frame, then played back to rest. Compatible properties such as color and corner radius morph along the way.

`Animate.Motion` plays an in-page clip on one view or many. A `Motion` value is an immutable recipe with no targets. `MotionPlayer` binds that recipe to views and can run it forward, in reverse, paused, repeated, staggered, sprung, or scrubbed.

The two families compose. The hero flies, and destination chrome starts from `HeroInFlight`. Fade, slide, and scale live on `Motion`. Give each property one writer. A `MotionPlayer` and MauiReactor `WithAnimation` on the same property of the same view fight each other.

Page flights default to **400 ms** and **`Easing.CubicOut`**. Motion defaults to **300 ms** and **`Easing.CubicOut`**.

## Getting started

Reactor.Animate is meant to work for a MauiReactor app.

### Install

1. Follow the [MauiReactor instructions](https://github.com/adospace/reactorui-maui#setting-up-mauireactor-from-cli) to create a MauiReactor app.
2. Install Reactor.Animate.

```
dotnet add package Reactor.Animate --prerelease
```

The package id is `Reactor.Animate`. The library targets .NET 10 and .NET MAUI.

### Examples

For a page hero, wrap the root page, tag matching views, and push through `Animate.Page`.

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
                .BackgroundColor(Colors.OrangeRed)
                .OnTapped(Open)
                .Hero("cover")
        )
        .HasNavigationBar(false);

    static Task Open()
        => Animate.Page.PushAsync<DetailPage>(t => t
            .Hero("cover", h => h.AnchorCenter())
            .WithEasing(Easing.CubicOut));
}

class DetailPage : Component
{
    public override VisualNode Render()
        => ContentPage(
            VStack(
                BoxView()
                    .HeightRequest(180)
                    .BackgroundColor(Colors.OrangeRed)
                    .Hero("cover"),
                Button("Back", async () => await Animate.Page.PopAsync())
            )
        )
        .HasNavigationBar(false);
}
```

For an in-page clip, bind a recipe with `Animate.Motion` and play it from the player.

```csharp
sealed class MotionAnimation : Component
{
    private MotionPlayer? _player;

    public override VisualNode Render()
        => ContentPage("Counter Sample",
            VStack(
                Label("This is awesome!")
                    .BindMotion(
                        Animate.Motion.Define(
                            m => m
                                .RotateX(180)
                                .WithDuration(500)
                                .WithEasing(Easing.CubicInOut)
                        ),
                        p => _player = p
                    ),

                HStack(
                    Button("Play Text Animation")
                        .OnClicked(async () => await _player.ForwardAsync()),
                    Button("Reverse Text Animation")
                        .OnClicked(async () => await _player.ReverseAsync())
                )
                .Spacing(10)
            )
            .Spacing(30)
            .Center()
        );
}
```

### Namespaces

Everything you call from an app is in two namespaces:

```csharp
global using Reactor.Animate;
global using Reactor.Animate.Animation;
```

| Namespace | What lives there |
|---|---|
| `Reactor.Animate` | `Animate`, `Animate.Page`, `Animate.Motion`, `.Hero()`, `.AnimateHost()`, `.BindMotion()`, the null-safe player extensions, `HeroTransitionEventArgs`, `MotionPlaybackStatus`, `MotionPlaybackEventArgs` |
| `Reactor.Animate.Animation` | `Transition`, `Motion`, `MotionPlayer`, `Spring`, `Stagger`, `SlideFrom`, `ColorSpace`, `KeyframeBuilder<T>` |

`Animate.Page` and `Animate.Motion` are nested classes. They are not namespaces.

The snippets below assume those two usings, plus `MauiReactor`.

### Host

Call `.AnimateHost()` once, around the root page. The first example does this on `HomePage`.

| Member | Role |
|---|---|
| `VisualNode.AnimateHost()` | Wraps the node in an `AnimatedHost`. This is the call to use. |
| `AnimatedHost` | Renders a MauiReactor `NavigationPage`, suppresses the platform slide, and wires system back. |

`Animate.Page` reads that navigation object. Calling push or pop before the host has mounted throws:

```text
Wrap the root page with .AnimateHost() before calling Animate.Page.
```

```
new HomePage().AnimateHost()
  └── NavigationPage
        ├── HomePage
        │     BoxView(...).Hero("cover")
        └── DetailPage
              BoxView(...).Hero("cover")
```

Hide the navigation bar on pages that fly (`HasNavigationBar(false)`). The host already handles system back. See [Platform](#platform).

## Page

A page transition is a `Transition` from `Reactor.Animate.Animation`. You build one inside the factory passed to `PushAsync`. The factory receives `Transition.None`.

```csharp
static Task Open()
    => Animate.Page.PushAsync<DetailPage>(t => t
        .Hero("cover", h => h.AnchorCenter())
        .WithDuration(400)
        .WithEasing(Easing.CubicOut));
```

### Push and pop

```csharp
await Animate.Page.PushAsync<DetailPage>();

await Animate.Page.PushAsync<DetailPage>(t => t.Hero("cover"));

await Animate.Page.PushAsync<DetailPage, DetailProps>(
    t => t.Hero("cover"),
    props => props.Id = id);

await Animate.Page.PopAsync();
```

| Member | Role |
|---|---|
| `PushAsync<TPage>(Func<Transition, Transition>? transitionFactory = null)` | Pushes `TPage` with `animated: false` and plays the transition. Omit the factory when nothing is shared. `TPage : Component, new()`. |
| `PushAsync<TPage, TProps>(Func<Transition, Transition> transitionFactory, Action<TProps> props)` | Same push, with MauiReactor props. The factory is required. `TProps : class, new()`. |
| `PopAsync()` | Plays the reverse flight, then pops. |

Both push methods return `Task<Page>` for the destination page. `PopAsync` returns `Task`.

Use these instead of `Navigation.PushAsync` / `PopAsync` for a pair that shares a hero. A raw platform push has no flight, and a raw platform pop can slide the page out from under the clip.

`PopAsync` returns immediately, and raises no events, when a flight is already running or the stack has one page. A push that arrives while a flight is running returns the page currently on top and does not start a second flight.

On pop, rotation and translation extras are negated. `Rotate(90)` on the way in is `-90` on the way out. `Translate(50, 0)` on the way in is `(-50, 0)` on the way out. The source heroes are inverted onto the destination frames, then played back to rest on the page that is being revealed.

Passing props, one cell from a grid:

```csharp
Animate.Page.PushAsync<GalleryDetailPage, GalleryItemProps>(
    t => t.Hero($"tile-{item.Id}", h => h.AnchorCenter()).WithDuration(300),
    props =>
    {
        props.Id = item.Id;
        props.Color = item.Color;
    });
```

The destination tags the same string: `.Hero($"tile-{Props.Id}")`. Read the cell at tap time. Do not keep a `CollectionView` cell around for the return flight. The pop measures the live cell.

### Heroes

```csharp
BoxView()
    .HeightRequest(72)
    .CornerRadius(12)
    .BackgroundColor(Colors.OrangeRed)
    .OnTapped(Open)
    .Hero("cover")
```

| Member | Role |
|---|---|
| `VisualNode.Hero(string tag)` | Marks the node as a shared element. The same tag on the destination is the other end of the pair. |

Put gesture handlers on the control, then call `.Hero()`. `Hero` returns the wrapper, so a handler chained after it attaches to that wrapper.

`.Hero` works on any `VisualElement`: `BoxView`, `Image`, `Border`, `Button`, a layout. The call wraps the node in a `Grid` and registers the first visual child of that grid as the flying view. The wrapper itself does not fly.

Tags are ordinal strings. A view has one tag. One push can fly several pairs, one pair per tag. A tag with no match on the other page is skipped.

The flight morphs the frame (position and size) and compatible properties such as background color and corner radius. An `Image` with the same `Source` and a similar `Aspect` looks like the photo growing or shrinking. A different `Source` cuts to the new bitmap. `AspectFit` against `AspectFill` can jump on the last frame, because the rect flies and the fit inside the rect does not.

### Transitions

`Transition` is immutable. Every method returns a new value. `Transition.None` is the empty start the factory receives. Its `Tags` list is empty.

```csharp
t => t
    .Hero("cover")
    .WithDuration(300)
    .WithEasing(Easing.CubicInOut)
```

| Member | Role |
|---|---|
| `Transition.None` | Empty transition. Duration 400, easing `CubicOut`, chrome fade on. |
| `.Hero(params string[] tags)` | Shared-element flight for those tags. Extras chained after this call apply to every tag in the call. |
| `.Hero(string tag, Func<Transition, Transition> configure)` | One tag, with its own anchor, rotation, and translation. |
| `.WithDuration(uint milliseconds)` | Clip length in milliseconds. |
| `.WithEasing(Easing easing)` | Clip easing. |
| `.Anchor(double x, double y)` | Scale and rotation origin. `(0, 0)` is the top-left. `(0.5, 0.5)` is the center. |
| `.AnchorCenter()` | `(0.5, 0.5)`. |
| `.AnchorTopLeft()` | `(0, 0)`. |
| `.AnchorTopRight()` | `(1, 0)`. |
| `.AnchorBottomLeft()` | `(0, 1)`. |
| `.AnchorBottomRight()` | `(1, 1)`. |
| `.Rotate(double degrees)` | Extra rotation, in degrees, baked into the invert and played back to rest. |
| `.Translate(double x, double y)` | Extra translation, in device-independent pixels, baked into the invert. |
| `.WithoutChromeFade()` | Leave non-hero chrome alone on this flight. |
| `left \| right` | Merge. See [Merge](#merge). |

`Duration`, `Easing`, and `Tags` are readable on the value you built. `Tags` is the distinct set of hero tags, in layer order.

#### Duration and easing

The default is 400 milliseconds, `Easing.CubicOut`.

```csharp
t => t.Hero("cover").WithDuration(300).WithEasing(Easing.SinOut)
```

`WithDuration` and `WithEasing` apply to the whole flight, including every hero tag and the chrome fade.

#### Anchors

The anchor is the origin of the scale (and of `.Rotate`) while the view covers the other frame.

```csharp
t => t.Hero("cover", h => h.AnchorCenter())
t => t.Hero("from_tl", h => h.AnchorTopLeft())
t => t.Hero("cover", h => h.Anchor(0.5, 0))
```

`(0, 0)` is the default. A merge treats an anchor of `(0, 0)` as unset, so `AnchorTopLeft()` does not override a center already stored on that layer. Set the origin inside the `Hero` configure when a tag needs a specific point.

#### Rotate and translate

```csharp
t => t.Hero("spin", h => h.AnchorCenter().Rotate(90).Translate(0, -24))
```

Both are extras on the invert. The clip plays them back to rest on push. Pop uses the negated angle and the negated offset.

These methods are on `Transition`. `Motion.Rotate` and `Motion.Translate` write the live view properties of an in-page clip. Same names, different types.

#### Several heroes

Each `.Hero` call appends a layer. A layer can name one tag or many. Chaining `.Anchor`, `.Rotate`, or `.Translate` updates the layer you just added.

```csharp
Animate.Page.PushAsync<DetailPage>(t => t
    .Hero("cover", h => h.AnchorCenter())
    .Hero("from_tl", h => h.AnchorTopLeft())
    .Hero("spin_90", h => h.AnchorCenter().Rotate(90))
    .WithEasing(Easing.CubicInOut));
```

`.Hero("c", "d").AnchorCenter()` puts the same origin on `c` and `d`. A later `.Hero("e", h => h.AnchorTopLeft())` does not change `c` or `d`.

If the same tag appears in more than one layer, the later layer supplies its extras.

#### Chrome fade

By default, non-hero content on the incoming page fades in. The fade starts at 70% of the clip (`Delay(0.7)` on the flight easing): opacity goes from 0 to 1 over the remaining time.

The walk skips:

- the flying heroes
- ancestors of those heroes
- any view whose `TranslationX` or `TranslationY` is already non-zero

Views with a non-zero translation are left alone so a staged entrance (`WithAnimation`, or a `Motion` that rests off-screen) can own them. There is no chrome fade when the transition has no matching heroes.

```csharp
await Animate.Page.PushAsync<DetailPage>(t => t
    .Hero("orb")
    .WithoutChromeFade());
```

`WithoutChromeFade()` is how destination chrome can rest at `TranslationX == 0` and still be played by `Animate.Motion`. The sample circle detail does this: the push opts out of the fade, the chrome is bound at opacity 0, and `HeroInFlight` starts it. See [Motion during a flight](#motion-during-a-flight).

#### Merge

`|` merges two transitions. Hero layers append. Timing uses a default as the sentinel:

- Duration: the right-hand value wins when it is not 400. Otherwise the left-hand duration is kept.
- Easing: the right-hand easing wins when it is not the default `CubicOut` instance. Otherwise the left-hand easing is kept.
- Anchor, rotation, and translation: a non-zero right-hand component wins. Zero keeps the left-hand component.
- Chrome fade: the result fades only when both sides still fade. Either `WithoutChromeFade()` turns it off.

```csharp
var spin = Transition.None.Hero("spin", h => h.Rotate(90)).WithDuration(500);
var cover = Transition.None.Hero("cover").WithEasing(Easing.SinOut);

var both = spin | cover;
```

`both` has both tags, a duration of 500 (the right-hand duration is still the default 400, so the left-hand 500 stays), and `SinOut` (the right-hand easing is not the default).

Chaining methods is the usual way to build a flight. `|` is there when two values are built separately.

### Events

`Animate.Page` raises three events for every flight that actually runs. Subscribe for the lifetime of the component. A new lambda on every tap is a new delegate and stacks. The same delegate instance is stored once.

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
```

| Event | When | What is safe |
|---|---|---|
| `HeroStarted` | `e.Page` is laid out. A hold frame still covers the window. Invert has not run. | Change page content, including hero layout. The next measure is what the invert sees. |
| `HeroInFlight` | The hold is gone and the clip is on screen. Raised once per flight, not on every frame. | Work that should appear with the morph. Layout of a flying hero is locked. Follow the curve with `e.At`. |
| `HeroEnded` | The clip finished, or there was nothing to play. Always raised after `HeroStarted`, including when a handler throws. | Follow-up work on the live page. Layout is unlocked. |

A handler that throws is logged to debug output and skipped. The flight continues, and `HeroEnded` is still raised.

A busy flight and a one-page `PopAsync` do not enter this sequence, so they raise nothing.

`HeroTransitionEventArgs`:

| Member | Meaning |
|---|---|
| `Kind` | `HeroTransitionKind.Push` or `HeroTransitionKind.Pop`. |
| `Page` | The destination page on push. The page being revealed on pop. |
| `Transition` | The transition that was played. |
| `Tags` | `Transition.Tags`. |
| `Progress` | 0–1 along the clip, in the flight easing. 0 at `HeroStarted`, 1 at `HeroEnded`. |
| `At(Action<double> callback)` | Invokes `callback` with `Progress` immediately, then on each later tick of this flight. |

```csharp
void OnHeroInFlight(object? sender, HeroTransitionEventArgs e)
{
    if (e.Kind != HeroTransitionKind.Push)
        return;

    e.At(t =>
    {
        if (t >= 0.7)
            SetState(s => s.ShowChrome = true);
    });
}

void OnHeroEnded(object? sender, HeroTransitionEventArgs e)
{
    if (e.Kind != HeroTransitionKind.Push || !e.Tags.Contains("cover"))
        return;

    // The flying views are unlocked. Follow-up layout belongs here.
}
```

Filter on `Kind` and `Tags`. The events are app-wide. Every mounted subscriber hears every flight.

### Progress

`Progress` uses the same easing as the flight (`e.Transition.Easing`). It is monotonic. `NaN` and infinities are ignored. A value less than or equal to the current progress is ignored. Listeners are copied before they run, and a listener that throws is logged and skipped.

Subscribe from `HeroStarted` or `HeroInFlight`. `At` on `HeroEnded` only sees the final `1`, because the clip has already finished. `At(1)` can still run while the layout lock is held. Work that needs the unlocked page belongs in `HeroEnded`.

`Progress` on a hero flight is independent of `MotionPlayer.SeekFraction`. Scrubbing a player does not move the flight, and the flight does not move the player.

### What a flight does

```
push                                         pop
snapshot source heroes                       snapshot destination heroes
hold the source frame                        hold the destination frame
push the destination, no platform slide      pop the destination, no platform slide
invert destination onto the source frames    invert source onto the destination frames
  plus Rotate / Translate                      plus the negated extras
HeroInFlight, hold lifts                     HeroInFlight, hold lifts
play the destination to rest                play the source to rest
```

The library animates the frame. `MotionPlayer` and `WithAnimation` animate what is inside the page.

Before the clip, ancestor layouts are unclipped and the flying view is raised, so a hero inside a `CollectionView` can grow past the cell. Clips and z-order are restored when the flight ends. The flying view itself keeps its own mask, so a `BoxView` corner radius stays round.

Heroes inside a virtualized `CollectionView` are measured in window space. Cell `Bounds` are often `(0, 0)` and are not the on-screen rect.

### While a hero is in flight

From `HeroInFlight` through the release before `HeroEnded`, the library:

- Reverts size, margin, alignment, and visibility changes on the flying heroes.
- Defers unregistration of those heroes, so a re-render cannot drop them mid-flight.
- Keeps writing the transforms the clip owns. Setting `Translation` or `Scale` on a flying hero in a handler does not stick.
- Skips `MotionPlayer` property writes on those heroes. The player can still capture its from-value. Writes resume after the flight. See [Motion during a flight](#motion-during-a-flight).

Non-hero chrome is not locked.

### Platform

| Platform | What the host does |
|---|---|
| iOS, Mac Catalyst | Disables the interactive edge-swipe on the navigation controller. That gesture would pop with the platform slide during the drag. Back runs through `PopAsync`, which plays the full reverse. |
| Android | Intercepts system back and predictive back, then calls `PopAsync`. On the root page, back still leaves the app. |

Hide the navigation bar on pages that fly. A custom in-page bar (the sample uses one) is ordinary chrome and can take part in the fade or in a `Motion`.

Use a MauiReactor `NavigationPage` for morphing pairs. The host is that navigation page. Shell is a different stack.

## Motion

A `Motion` is an immutable recipe. It stores tracks, timing, and playback options. It does not store views. Bind it when you play it.

```csharp
static readonly Motion Pulse = Animate.Motion.Define(m => m
    .Scale(1, 1.08)
    .WithDuration(180));
```

`Animate.Motion.Define` starts from `Motion.None` and returns whatever the function returns. `Motion.None` is 300 ms, `Easing.CubicOut`, one play, HSV color, no spring, no stagger, no perspective.

### Recipes

Tracks write bindable properties. A to-only call captures the live value as `from` on the first `Forward`. A from/to call writes both ends, and the first `Forward` applies `from` before the clock starts. A third overload takes keyframes. See [Keyframes](#keyframes).

```csharp
Animate.Motion.Define(m => m
    .FadeIn()
    .Scale(0.9, 1)
    .WithDuration(280));
```

`Motion.Translate` / `Motion.Rotate` write view properties. `Transition.Translate` / `Transition.Rotate` are FLIP extras on a page flight.

### Property tracks

#### Opacity

```csharp
m.Opacity(1)            // from the live value to 1
m.Opacity(0, 1)         // from 0 to 1
m.Opacity(k => k.At(0, 0).At(0.35, 1).At(1, 1))
```

Writes `VisualElement.Opacity`.

#### Translation

```csharp
m.Translate(40, 0)                 // TranslationX 40, TranslationY 0
m.Translate(-40, 0, 0, 0)          // from (-40, 0) to (0, 0)
m.TranslateX(-100, 0)
m.TranslateY(24)
m.TranslateX(k => k.At(0, -40).At(1, 0))
```

Values are device-independent pixels. `Translate(x, y)` is `TranslateX` then `TranslateY`.

#### Scale

```csharp
m.Scale(1.08)
m.Scale(0.85, 1)
m.ScaleX(1, 1.2)
m.ScaleY(1, 0.8)
m.Scale(k => k.At(0, 0.8).At(0.6, 1.08).At(1, 1))
```

`Scale` writes `ScaleX` and `ScaleY`. It does not write `Scale`.

#### Rotation

```csharp
m.Rotate(180)
m.Rotate(0, 180)
m.Rotate(k => k.At(0, 0).At(1, 360))
```

Degrees, clockwise, `VisualElement.Rotation`. This is the in-plane spin.

#### Depth

`RotateX` and `RotateY` are degrees about the horizontal and vertical axes, around `AnchorX` and `AnchorY` (defaults `0.5, 0.5`).

`Perspective(entry)` sets the eye distance for that rotation. `entry` is the same number Flutter writes with `Matrix4.setEntry(3, 2, entry)`. The camera sits `1 / |entry|` device-independent pixels from the plane. `0.001` shrinks the far edge a little. `0` is orthographic: both edges stay the same height. `Perspective` is a constant on the recipe. It is not a track, and it does not animate by itself.

```csharp
Motion.None
    .Perspective(0.004)
    .RotateY(0, 60)
    .WithDuration(400)
```

Call `Perspective` together with `RotateX` or `RotateY`. On iOS and Mac Catalyst the recipe writes `CATransform3D.m34 = -entry` after the platform builds its transform, using the same anchor, translation, rotation, and scale composition as MAUI. On Android the same eye distance is passed to `CameraDistance`, scaled by `density² × √5`, so the foreshortening matches that entry. An entry of `0` parks the Android camera far enough that the plane does not foreshorten.

On Windows, `RotateX` and `RotateY` still run through MAUI's plane projection, at the platform distance. `Perspective(entry)` does not change that distance.

A later recipe on the same view replaces the entry. `|` keeps the right-hand entry when the right-hand motion has one.

#### Color

```csharp
m.BackgroundColor(Colors.Lime)
m.BackgroundColor(Colors.Red, Colors.Lime)
m.BackgroundColor(k => k.At(0, Colors.Red).At(1, Colors.Lime))
```

Writes `VisualElement.BackgroundColor`. Interpolation is HSV by default. See [Color space](#color-space).

#### Size

```csharp
m.Width(120)
m.Width(72, 180)
m.Height(72, 180)
m.Height(k => k.At(0, 72).At(1, 180))
```

Writes `WidthRequest` and `HeightRequest`.

#### Corner radius

```csharp
m.CornerRadius(24)
m.CornerRadius(new CornerRadius(12), new CornerRadius(24))
m.CornerRadius(k => k.At(0, 12).At(1, 24))
```

On a `BoxView` this writes `CornerRadius`. On a `Border` it writes `StrokeShape` as a `RoundRectangle`. Other target types are skipped. A debug build logs the skip.

#### Any property

```csharp
m.Property(VisualElement.OpacityProperty, 1)
m.Property(VisualElement.RotationProperty, 0d, 45d)
```

`Property` adds a track for a `BindableProperty` whose type is `double`, `float`, `Color`, `Thickness`, `CornerRadius`, or `Rect`, and for a `Border` stroke shape. Pass `from` when the start value must be explicit. Leave it out to capture the live value on the first `Forward`.

### Named recipes

The named helpers are ordinary tracks with a preset from and to.

| Member | Writes |
|---|---|
| `FadeIn()` | Opacity 0 → 1 |
| `FadeOut()` | Opacity 1 → 0 |
| `SlideIn(SlideFrom edge, double distance = 24)` | Translation from off-screen to 0 |
| `SlideOut(SlideFrom edge, double distance = 24)` | Translation from 0 to off-screen |
| `ScaleIn(double from = 0.85)` | Scale `from` → 1 |
| `ScaleOut(double to = 0.85)` | Scale 1 → `to` |

```csharp
m.FadeIn().SlideIn(SlideFrom.Left, 40)
m.SlideOut(SlideFrom.Bottom)
m.ScaleIn()
```

`SlideFrom` is `Left`, `Right`, `Top`, or `Bottom`. `Left` and `Top` use a negative translation. `Right` and `Bottom` use a positive one. The distance is in device-independent pixels.

`SlideIn` moves to translation 0. Park the view on that off-screen translation yourself when another system (the page chrome fade) would also touch it. A non-zero `TranslationX` or `TranslationY` is how a view opts out of the chrome fade. See [Chrome fade](#chrome-fade).

### Timing

```csharp
m.WithDuration(280).WithEasing(Easing.CubicOut)
```

| Member | Role |
|---|---|
| `Duration` | Length in milliseconds. Default 300. |
| `Easing` | Default `Easing.CubicOut`. |
| `WithDuration(uint milliseconds)` | Sets the length. |
| `WithEasing(Easing easing)` | Sets the easing. `null` throws. |

On a recipe whose tracks all span the full motion, `WithDuration` replaces the length. Keyframe offsets are fractions of that length, so they stretch with it.

On a recipe that already has windows (a timeline, or anything whose tracks are not `[0, 1]`), `WithDuration` only grows the clock. A shorter value is ignored. Existing windows stay the same number of milliseconds and the extra time is padding at the end. Leaf motions are not scaled to fill the new length.

`WithDuration` and `WithSpring` are mutually exclusive. A debug build asserts if you call `WithDuration` on a spring recipe. Playback follows the spring until rest. See [Springs](#springs).

### Color space

```csharp
public enum ColorSpace
{
    Hsv,
    Rgb,
}
```

```csharp
m.BackgroundColor(Colors.Red, Colors.Lime)                         // HSV
m.BackgroundColor(Colors.Red, Colors.Lime).WithColorSpace(ColorSpace.Rgb)
```

| Member | Role |
|---|---|
| `WithColorSpace(ColorSpace colorSpace)` | Selects how `Color` tracks blend. Default `ColorSpace.Hsv`. |

HSV interpolates hue along the short arc. Saturation and value blend in that space. Alpha stays linear. A color with saturation below `1e-6` keeps the other stop's hue, so a grey does not spin the hue channel.

RGB blends each channel, including alpha, in a straight line. Red to lime passes through the middle of the cube.

`|` takes the right-hand color space.

### Bind and play

`Animate.Motion` is the facade. The recipe type is `Motion`. The player type is `MotionPlayer`.

```csharp
MotionPlayer player = Animate.Motion.Bind(
    Animate.Motion.Define(m => m.FadeIn().WithDuration(280)),
    view);

player.Forward();
```

| Member | Role |
|---|---|
| `Define(Func<Motion, Motion> configure)` | `configure(Motion.None)`. |
| `Bind(Motion motion, VisualElement target)` | Bind one view. Does not start. |
| `Bind(Motion motion, IEnumerable<VisualElement> targets)` | Bind many views. Does not start. The order is the stagger order. |
| `Bind(Func<Motion, Motion> configure, VisualElement target)` | Define, then bind. |
| `Bind(Func<Motion, Motion> configure, IEnumerable<VisualElement> targets)` | Define, then bind. |
| `Play(Motion motion, params VisualElement[] targets)` | Bind and start forward. Returns the player. |
| `Play(Func<Motion, Motion> configure, VisualElement target)` | Define, bind, start. |
| `Play(Func<Motion, Motion> configure, IEnumerable<VisualElement> targets)` | Define, bind, start. |
| `ForwardAsync(Motion / configure, target / targets, CancellationToken cancellationToken = default)` | Bind and play forward. Returns the `Task` only. |
| `ReverseAsync(Motion / configure, target / targets, CancellationToken cancellationToken = default)` | Bind and play reverse. Returns the `Task` only. |

`Motion` has the same `Bind` overloads as instance methods:

```csharp
var player = Motion.None.FadeIn().WithDuration(280).Bind(view);
```

`Play` is bind, then `Forward()`, and you keep the player so you can pause or reverse it later.

```csharp
_player = Animate.Motion.Play(m => m.Scale(1, 1.08).WithDuration(180), card);
```

`ForwardAsync` and `ReverseAsync` on the facade also bind and start, and the only handle you get back is the `Task`. Use `Bind` or `Play` when you need the player. A later `Bind` or `Play` on that view disposes the player the one-shot call left behind. See [One player per view](#one-player-per-view).

The facade methods accept a `CancellationToken`. Cancellation does not throw out of these methods. See [Forward and reverse](#forward-and-reverse).

### BindMotion

```csharp
Label("Hello")
    .BindMotion(
        Animate.Motion.Define(m => m.RotateY(0, 180).Perspective(0.004).WithDuration(500)),
        player => _player = player)
```

| Member | Role |
|---|---|
| `VisualNode.BindMotion(Motion motion, Action<MotionPlayer>? onBind = null)` | Binds on `Loaded`. Disposes the player on `Unloaded`. Does not start, whether or not you pass `onBind`. |

`onBind` is where you keep the player and subscribe to it. Call `Forward()` yourself.

The extension wraps the node in a `Grid`. When that grid has a single visual child, and that child is itself a single-child layout, the bind walks to the innermost visual and targets that. A layout with several visual children is the target itself. Put `BindMotion` on the view you want to move.

```csharp
Button("Pulse")
    .BindMotion(Pulse, player => _pulse = player)
    .OnTapped(() =>
    {
        if (_pulse is null)
            return;

        if (_pulse.Status == MotionPlaybackStatus.Completed)
            _pulse.Reverse();
        else
            _pulse.Forward();
    });
```

Dispose is automatic when the node unloads. If you also hold the player, drop your field in `OnWillUnmount` so you do not call a disposed player later. The null-safe extensions make that call a no-op. See below.

### MotionPlayer

```csharp
public sealed class MotionPlayer : IDisposable
{
    public Motion Motion { get; }
    public MotionPlaybackStatus Status { get; }
    public double Progress { get; }
    public bool IsRunning { get; }
    public uint Duration { get; }

    public event EventHandler<MotionPlaybackEventArgs>? Started;
    public event EventHandler<MotionPlaybackEventArgs>? Completed;
    public event EventHandler<MotionPlaybackEventArgs>? Paused;
    public event EventHandler<MotionPlaybackEventArgs>? Resumed;
    public event EventHandler<MotionPlaybackEventArgs>? StatusChanged;

    public void At(Action<double> callback);
    public void Pause();
    public void Resume();
    public void Reset();
    public void Seek(uint milliseconds);
    public void Seek(string id);
    public bool TrySpan(string id, out double begin, out double end);
    public void SeekFraction(double t);
    public void Dispose();
}
```

`Duration` is the recipe length plus the longest stagger delay. A 300 ms motion with a 100 ms stagger across two views has `Duration == 400`.

`Progress` is eased playback in the range 0–1. It increases on the way forward and decreases on the way back. A spring reports the spring position, clamped to 0–1. `IsRunning` is true when `Status` is `Forward` or `Reverse`.

`At` registers a callback for the life of the player. It runs immediately with the current `Progress`, then on every later tick, including ticks that decrease. A callback that throws is logged and skipped.

```csharp
player.At(t => scrubLabel.Text = $"{t:0.00}");
```

#### Forward and reverse

The playback methods that start a run are extensions on `MotionPlayer?`, in `Reactor.Animate`:

```csharp
_player.Forward();
_player.Reverse();
await _player.ForwardAsync();
await _player.ReverseAsync(cancellationToken);
```

| Member | Role |
|---|---|
| `Forward()` | Starts forward. Fire-and-forget. |
| `Reverse()` | Starts reverse. Fire-and-forget. |
| `ForwardAsync(CancellationToken cancellationToken = default)` | Starts forward and returns a task for that run. |
| `ReverseAsync(CancellationToken cancellationToken = default)` | Starts reverse and returns a task for that run. |

All four are safe on a null player and on a disposed player. They do not throw `OperationCanceledException` or `ObjectDisposedException`. `Reset`, `Dispose`, and a canceled token cancel the pending task. The extension finishes successfully anyway. `Pause` does not cancel it. The task stays pending until the run reaches an end, `Resume` finishes it, or something cancels it.

A completed `Task` is not proof the clip reached the end. After an awaited play, read `Status`:

```csharp
await _player.ForwardAsync();
if (_player?.Status == MotionPlaybackStatus.Completed)
{
    // The run reached the end.
}
```

Behavior of a live player:

| Call | Result |
|---|---|
| `Forward` from `Dismissed` | Captures `from`, then plays forward. Auto-repeat is on, so `Repeat` / `Yoyo` continue. |
| `Forward` while already forwarding | Joins the in-flight task. |
| `Forward` from `Paused` after a forward | Continues that forward run. Does not capture `from` again. |
| `Forward` from `Completed` | No-op. The task is already complete. |
| `Forward` while reversing, or paused from a reverse | The reverse task completes successfully. Playback turns around without capturing `from` again. Auto-repeat is on. |
| `Reverse` from `Dismissed` | No-op. There is no captured run to reverse. |
| `Reverse` from `Completed` or mid-forward | Plays backward from the current position. Turns auto-repeat off, so a yoyo stops at the end of this reverse. |
| `ForwardAsync` / `ReverseAsync` with a token that is already canceled | `Started` fires, then the player pauses and the task is canceled. The extension completes without throwing. |

An empty target list completes `Forward` immediately (`Completed`) and leaves `Reverse` at `Dismissed`.

A duration of 0 finishes on the first tick and does not auto-repeat. `Repeat` and `Yoyo` wait until that only when the duration is non-zero, or when a spring is settling.

#### Pause resume and reset

```csharp
_player.Pause();
_player.Resume();
_player.Reset();
_player.Dispose();
```

| Member | Role |
|---|---|
| `Pause()` | Stops the clock when the player is running. Status becomes `Paused`. Other statuses are left alone. |
| `Resume()` | Continues a paused run in the direction it had. No-op when the status is not `Paused`. |
| `Reset()` | Stops the clock, cancels the pending task, writes every track back to its captured `from`, and sets `Dismissed`. |
| `Dispose()` | Stops, cancels, and unregisters the player from its views. A second call is a no-op. |

`Pause` stops the clock and leaves the in-flight `ForwardAsync` / `ReverseAsync` task pending. `Resume` raises `Resumed` and continues that same task. If the task was already canceled (by `Reset`, `Dispose`, or the token), `Resume` starts a new run from the paused position without capturing `from` again.

`Reset` before the first `Forward` has nothing captured, so it only returns the status to `Dismissed`.

#### Status

```csharp
public enum MotionPlaybackStatus
{
    Dismissed,
    Forward,
    Reverse,
    Paused,
    Completed,
}
```

| Status | Meaning |
|---|---|
| `Dismissed` | No run, or `Reset` put the views back at `from`. Also the end of a reverse. |
| `Forward` | The clock is playing toward the end. |
| `Reverse` | The clock is playing toward the start. |
| `Paused` | Stopped in the middle by `Pause`, by a cancel, or by a seek inside `(0, 1)`. |
| `Completed` | The forward run reached the end, including the last repeat. |

`MotionPlaybackEventArgs` carries the recipe, the targets, the status, and `Progress` for the event that just fired.

#### Player events

```csharp
player.Started += (_, e) => { /* e.Status is Forward or Reverse */ };
player.Completed += (_, e) => { /* a forward pass reached the end */ };
player.Paused += (_, _) => { };
player.Resumed += (_, _) => { };
player.StatusChanged += (_, e) => { };
```

| Event | When |
|---|---|
| `Started` | A run begins, including each repeat cycle and a direction change. |
| `Completed` | A forward pass reaches the end. A yoyo raises this before the automatic reverse. |
| `Paused` | `Pause`, a cancel that parks the player, or a seek that stops inside the span. Raised once when leaving `Forward` or `Reverse`, not on every scrub tick. |
| `Resumed` | `Resume` continues a paused run that still has a pending task. |
| `StatusChanged` | Any status write. |

`e.At(callback)` on the event args follows that run's progress, including decreases. It is the per-run cousin of `MotionPlayer.At`. Hero flight progress is monotonic. Motion progress is not.

`e.Motion` is the recipe. `e.Targets` is the bound views.

### Seek

Seeking parks the player. It does not start playback, and it does not continue a `Repeat` or `Yoyo`. Lifting a slider does not play. Call `Forward` when you want the rest of the clip.

```csharp
player.Seek(150);          // 150 ms on the linear player clock
player.SeekFraction(0.5); // eased progress, the same units as Progress
player.Seek("pulse");      // start of a named timeline child
```

| Member | Role |
|---|---|
| `Seek(uint milliseconds)` | Linear wall-clock position. `0` dismisses. A value at or past `Duration` completes. |
| `SeekFraction(double t)` | Eased progress, clamped to 0–1. `NaN` and infinities seek to 0. |
| `Seek(string id)` | Linear seek to the start of a named `Add` / `Then` child. An unknown id does nothing. |
| `TrySpan(string id, out double begin, out double end)` | Linear 0–1 window of that child inside `Duration` (after stagger). Returns false when the id is missing. |

`SeekFraction` inverts `Motion.Easing` and then seeks that linear position, so the pixels match `Progress` and `At`. `Linear`, `CubicIn`, `CubicOut`, `CubicInOut`, `SinIn`, and `SinOut` invert in closed form. Any other easing is inverted by a 24-step search. If that search cannot invert the curve, the seek uses `t` as the linear position and a debug build logs it once.

A seek strictly inside the span sets `Paused` and raises `Paused` once when the player was running. Seeking again while already paused does not raise `Paused` again. Seeking from `Dismissed` or `Completed` into the middle also sets `Paused`, without a `Paused` event. `t == 1` completes. `t == 0` dismisses and writes the from-values.

`Seek(id)` and `TrySpan` use the linear player clock, the same clock as `Seek(uint)`. They are not eased. Named windows are fractions of the recipe duration, then mapped through the stagger padding onto `player.Duration`. With no stagger the two clocks match.

```csharp
if (player.TrySpan("pulse", out var begin, out var end))
{
    // begin and end are 0–1 of player.Duration
}
```

### Repeat and yoyo

```csharp
m.Scale(1, 1.12).Yoyo().Repeat(-1)
m.FadeIn().Repeat(2)
m.Rotate(0, 180).Yoyo()
```

| Member | Role |
|---|---|
| `Repeat(int count)` | How many forward passes to play. `1` is the default. `0` plays once. A negative count repeats until `Pause`, `Reset`, or `Dispose`. |
| `Yoyo(bool enabled = true)` | After each forward pass, play the reverse. One repeat is forward plus reverse. |

The player continues by itself. It does not recapture `from` between cycles. The `Task` from `ForwardAsync` stays pending until the last cycle. `Pause` leaves that task pending. `Reset`, `Dispose`, and a canceled token cancel it, and the extension finishes without throwing.

`Completed` fires at the end of each forward pass. `Started` fires again for the next leg.

`|` keeps the right-hand repeat when it is not `1`, otherwise the left-hand repeat. Yoyo is on if either side is on.

Calling `Reverse` yourself turns auto-repeat off, so a pulsing yoyo stops when the user reverses it. The next `Forward` turns auto-repeat back on.

Dispose a player that repeats forever when its page unloads. `BindMotion` does that on `Unloaded`. A player you created with `Bind` or `Play` needs `Dispose` in `OnWillUnmount`.

### Parallel merge

`|` plays both recipes on one clock. The parent length is `max(left.Duration, right.Duration)`. Tracks keep their millisecond length. They are not stretched to the parent. The left-hand tracks are flattened first, then the right-hand tracks. On an overlap, the later track wins. A debug build logs the overlap.

```csharp
var enter = Motion.None.FadeIn().WithDuration(280);
var grow = Motion.None.Scale(0.9, 1).WithDuration(400);

var both = enter | grow;   // 400 ms. Fade occupies the first 280.
```

| Piece | Which side wins |
|---|---|
| Length | The longer one. |
| Easing | The right-hand easing, when it is not the default `CubicOut`. Otherwise the left. |
| Stagger | The right-hand stagger, when it has one. Otherwise the left. |
| Repeat | The right-hand count, when it is not `1`. Otherwise the left. |
| Yoyo | On if either side is on. |
| Color space | The right-hand space. |
| Spring | The right-hand spring, when it has one. Otherwise the left. |
| Perspective | The right-hand entry, when it has one. Otherwise the left. |
| Named spans | Both, rebased into the parent. `|` does not assign new ids. |

`|` does not take a name. Use `Add` when the child needs an id.

### Stagger

Stagger delays each bound target on the linear player clock. It applies to the root motion, and only when more than one view is bound. A `Stagger` on a child passed to `Add` or `Then` is ignored. A debug build logs that.

```csharp
await Animate.Motion.ForwardAsync(
    m => m
        .FadeIn()
        .Scale(0.9, 1)
        .Stagger(40, StaggerFrom.Start, (3, 4))
        .WithDuration(280),
    tiles);
```

| Member | Role |
|---|---|
| `Stagger(uint stepMilliseconds, StaggerFrom from = StaggerFrom.Start, (int Columns, int Rows)? grid = null)` | Delay between targets. |
| `Stagger` | The value type: step, origin, optional grid. |
| `StaggerFrom` | `Start`, `Center`, or `End`. |

The player length becomes `Duration + max delay`. Each target's window is `[delay, delay + Duration)` on that longer clock. Before its window the target holds `from`. At time 0 every target is already at `from`.

A 300 ms motion, a 100 ms step, and two views produces a 400 ms player. The first window is `[0, 300)`, the second is `[100, 400)`.

`StaggerFrom.Start` delays `index * step`. `End` delays `(count - 1 - index) * step`. `Center` on a list delays by the distance from the middle index. Those three stay in bind order even when a grid is set, except `Center`: with a grid, `Center` uses the Euclidean distance from the cell to the center of the grid. Column and row come from the index in row-major order (`index % columns`, `index / columns`). The sample stagger page is 12 tiles, 3 columns by 4 rows, step 40.

`WithSpring` and `Stagger` are mutually exclusive. A debug build asserts if both are set. The spring clocks a single position for the player.

### Keyframes

Keyframe offsets are fractions of this motion, from 0 to 1. They stretch when `WithDuration` replaces the length of a full-span recipe. `NaN` and infinities become 0. Values outside 0–1 are clamped. Offsets must be non-decreasing. A debug build asserts when one goes backwards, and the offset is then clamped up to the previous one.

Per-property form:

```csharp
m.Opacity(k => k.At(0, 0).At(0.35, 1).At(1, 1))
 .Scale(k => k.At(0, 0.8).At(0.6, 1.08, Easing.CubicOut).At(1, 1))
```

`KeyframeBuilder<T>.At(double offset, T value, Easing? easing = null)` appends a stop and returns the builder so calls chain. The easing on a stop is the curve into that stop. A null easing uses the motion easing.

When the first offset is greater than 0, the track holds `from` until that offset.

Whole-state form, several properties at the same offsets:

```csharp
m.Keyframes(
    (0.00, s => s.Scale(1)),
    (0.40, s => s.Scale(1.14)),
    (1.00, s => s.Opacity(1).Scale(1)))
```

Each entry is `(double At, Func<Motion, Motion> Set)`. `Set` receives `Motion.None` and the tracks it returns become stops at that offset. A property omitted from a later state holds its last keyframed value. It does not blend back to the implicit from. An empty list, or a builder you never call `At` on, leaves the recipe unchanged.

Sequential steps in milliseconds belong on `Then`, not on `Keyframes`.

### Timeline

`Add` places a child on the parent clock. `Then` places it at the current end.

```csharp
var intro = Motion.None
    .FadeIn()
    .SlideIn(SlideFrom.Left, 48)
    .WithDuration(280);

var pulse = Motion.None
    .Scale(1, 1.12)
    .WithDuration(200);

var timeline = Motion.None
    .WithDuration(280)
    .Add(intro, at: 0, id: "intro")
    .Then(pulse, id: "pulse");

player.Seek("pulse");
```

| Member | Role |
|---|---|
| `Add(Motion child, uint at = 0, string? id = null)` | Inserts `child` at `at` milliseconds. Parent length becomes `max(current, at + child.Duration)`. |
| `Then(Motion next, string? id = null)` | `Add(next, Duration, id)`. |

Children are flattened immediately. Their tracks are shifted in milliseconds and are not stretched. A nested `Stagger` is ignored. If two tracks write the same property in overlapping time, the later one wins and a debug build logs the overlap.

`id` records a named window as a fraction of the parent length. Duplicate ids keep the last one. `Seek(id)` and `TrySpan` read those windows. See [Seek](#seek).

`Motion.None` is already 300 ms long, even with no tracks. Adding a 100 ms child onto a bare `None` produces a 300 ms parent, and the child occupies the first 100 ms. Set `WithDuration` on the base, or on the children before you add them, when that padding is not what you want. The sample timeline starts from `Motion.None.WithDuration(280)` so the intro window matches the intro.

`|` rebases named windows into the merged length. It does not give a merged recipe a new id. Pass the id to `Add` or `Then`.

### Springs

A spring plays a mass-spring-damper until rest. It is not an `Easing`. The wall-clock `Duration` is not the length of the clip.

```csharp
public readonly record struct Spring(double Stiffness, double Damping, double Mass)
{
    public static Spring Default { get; }  // 180, 16, 1
    public static Spring Snappy { get; }   // 400, 22, 1
    public static Spring Gentle { get; }   // 90, 18, 1
}
```

The parameterless constructor is `Default`.

```csharp
m.Scale(1, 1.16).WithSpring(Spring.Snappy)
m.TranslateX(0, 40).WithSpring(new Spring(220, 18, 1))
```

| Member | Role |
|---|---|
| `WithSpring(Spring spring)` | Play this recipe with that spring. |

`WithSpring` is mutually exclusive with `WithDuration` and with `Stagger`. A debug build asserts if you combine them.

The integrator is semi-implicit Euler. The step is clamped to 1/30 of a second. `Progress` is the spring position clamped to 0–1. The spring settles when position and velocity are both within `0.002` of rest, or after 8 seconds, whichever comes first. A settled forward pass then honors `Repeat` and `Yoyo`.

`|` keeps the right-hand spring when the right-hand motion has one.

### Path

`Path` writes `TranslationX` and `TranslationY` by sampling a `PathGeometry` along its length.

```csharp
static PathGeometry Bezier()
{
    var geometry = new PathGeometry();
    var figure = new PathFigure { StartPoint = new Point(0, 0) };
    figure.Segments.Add(new BezierSegment
    {
        Point1 = new Point(70, -90),
        Point2 = new Point(170, 90),
        Point3 = new Point(260, 0),
    });
    geometry.Figures.Add(figure);
    return geometry;
}

Animate.Motion.Bind(
    m => m.Path(Bezier()).WithDuration(900).WithEasing(Easing.SinInOut),
    orb);
```

| Member | Role |
|---|---|
| `Path(PathGeometry geometry, double from = 0, double to = 1)` | Move along `geometry`. `from` and `to` are fractions of the path length. |

`from` and `to` are clamped to 0–1. A non-finite `from` becomes 0. A non-finite `to` becomes 1. `geometry` must not be null.

The sampler flattens lines, polylines, quadratic Béziers, cubic Béziers, and elliptical arcs. A degenerate arc (a radius that collapses) becomes a straight line. The first `Forward` writes the point at `from`.

Stagger delays when each target starts walking the path. Keyframes along a path are a sequence of `Then` segments, each with its own geometry.

`Path` and `Translate` both write translation. Overlapping windows follow the later track.

An arc in the same shape as the sample:

```csharp
static PathGeometry Arc()
{
    var geometry = new PathGeometry();
    var figure = new PathFigure { StartPoint = new Point(0, 0) };
    figure.Segments.Add(new ArcSegment
    {
        Point = new Point(240, 0),
        Size = new Size(120, 90),
        SweepDirection = SweepDirection.Clockwise,
        IsLargeArc = false,
    });
    geometry.Figures.Add(figure);
    return geometry;
}
```

### One player per view

One live `MotionPlayer` owns a `VisualElement`. Binding a second player to that view disposes the first. The dispose happens after the new player is registered, and a debug build logs the steal.

The table is per view, not per property. A stagger player that targets twelve tiles owns all twelve. Binding a new player to any one of them disposes the whole stagger player.

`Dispose` unregisters only when this player is still the owner. Disposing a player that has already been replaced does not drop the replacement.

`Reset` and `Dispose` both cancel an in-flight `ForwardAsync`. Unload paths should dispose. The null-safe `Forward()` on a field you have already nulled out is a no-op.

### Motion during a flight

Hero flights pin the flying views from `HeroInFlight` until the clip ends. A `MotionPlayer` aimed at a pinned view still captures `from`. It skips property writes for the rest of the flight, then writes again on the next tick after the pin lifts. Chrome is not pinned.

The usual split: the hero flies, and destination chrome is a `Motion` on a different view.

```csharp
// Source page. Opt out of the automatic chrome fade.
await Animate.Page.PushAsync<DetailPage>(t => t
    .Hero("orb")
    .WithoutChromeFade());
```

```csharp
// Destination page.
VStack(
        Label("From the center"),
        Button("Back", async () => await Animate.Page.PopAsync())
    )
    .Opacity(0)
    .TranslationX(-100)
    .BindMotion(
        Animate.Motion.Define(m => m.FadeIn().TranslateX(-100, 0).WithDuration(300)),
        player => _chrome = player)
```

Start that player from the flight, partway along:

```csharp
void OnHeroInFlight(object? sender, HeroTransitionEventArgs e)
{
    if (e.Kind != HeroTransitionKind.Push)
        return;

    e.At(t =>
    {
        if (t >= 0.7)
            _chrome.Forward();
    });
}
```

`WithoutChromeFade` matters when the chrome rests at translation 0. With the default fade, a view whose translation is already non-zero is skipped, and a view at translation 0 is faded by the flight. Pick one owner for that opacity.

`_chrome.Forward()` is safe when the field is still null. The extension returns immediately.

## Samples

`samples/Sample` is a MauiReactor gallery: home heroes, CollectionView tiles, Photos (Image-to-Image), Circle chrome, Motion playground, stagger, scrub, color spaces, timeline seek, and path (Bézier + arc).

## Building

```
dotnet test tests/MauiAnimate.Tests/MauiAnimate.Tests.csproj
```

CI runs tests on Ubuntu (`net10.0`) and builds `net10.0-android` and `net10.0-ios`.

## License

MIT
