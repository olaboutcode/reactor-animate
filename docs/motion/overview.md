# Motion

`Motion` is an immutable recipe with no targets. Bind it to any `VisualElement` (or `VisualNode` via `BindMotion`). Playback lives on `MotionPlayer`. Defaults: **300 ms**, **`Easing.CubicOut`**. A second bind on the same view disposes the previous player.

```csharp
static readonly Motion Pulse = Animate.Motion.Define(m => m
    .Scale(1, 1.08)
    .WithDuration(180));

Button("Pulse")
    .BindMotion(Pulse, p => _pulse = p)
    .OnTapped(() =>
    {
        if (_pulse is null) return;
        if (_pulse.Status == MotionPlaybackStatus.Completed)
            _pulse.Reverse();
        else
            _pulse.Forward();
    });
```

Use `WithAnimation` when a state flag should morph layout. Use `MotionPlayer` when you need reverse, pause, stagger, or a recipe reused on any view. Driving the same property with both fights.

Hero flights fade non-hero chrome by default. Skip that with `WithoutChromeFade()` so dest Motion chrome can rest at `TranslationX` 0.

```csharp
await Animate.Page.PushAsync<Detail>(t => t.Hero("orb").WithoutChromeFade());

VStack(…)
    .Opacity(0)
    .BindMotion(
        Animate.Motion.Define(m => m.FadeIn().TranslateX(-100, 0).WithDuration(300)),
        p => _chrome = p);
```

Start chrome from `HeroInFlight`. See [Events](../page/events.md).
