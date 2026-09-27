# Transitions

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

Hero flights fade non-hero chrome by default after the shared-element clip is more than halfway through. Views with `TranslationX` / `TranslationY` ≠ 0 are still skipped.
