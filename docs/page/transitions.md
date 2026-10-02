# Transitions

`HeroTransition` is the page flight. `Hero` is one tagged view. Both are immutable. A later duration or easing replaces the default. Hero layers append, so each tag can keep its own anchor and rotation.

```csharp
ht => ht
    .Hero("cover", h => h.AnchorCenter())
    .Hero("from_tl", h => h.AnchorTopLeft())
    .Hero("spin", h => h.AnchorCenter().Rotate(90))
    .Hero(["c", "d"], h => h.AnchorCenter())
    .WithDuration(300)
    .WithEasing(Easing.CubicInOut)
```

The factory receives an empty `HeroTransition`. `h` has the anchor, rotation, and translation for that tag. It does not have `.Hero()`, duration, or easing.

| Member | Role |
|---|---|
| `.Hero(params string[] tags)` | Flies those tags with no extras. |
| `.Hero(string tag, Func<Hero, Hero> configure)` | One tag with its own anchor, rotation, or translation. |
| `.Hero(IReadOnlyList<string> tags, Func<Hero, Hero> configure)` | Several tags that share one `Hero`. |
| `.Anchor(x, y)` | On `Hero`. Origin for the frame scale and rotation. `(0, 0)` is top-left, `(0.5, 0.5)` is center. |
| `.AnchorCenter()` `.AnchorTopLeft()` `.AnchorTopRight()` `.AnchorBottomLeft()` `.AnchorBottomRight()` | Named origins on `Hero`. |
| `.Rotate(degrees)` | On `Hero`. Adds rotation to the invert, then plays back to rest. Pop uses `-degrees`. |
| `.Translate(x, y)` | On `Hero`. Extra translation on the invert, in device-independent pixels. Pop uses `(-x, -y)`. |
| `.WithDuration(uint milliseconds)` | On `HeroTransition`. Clip length. Default `400`. |
| `.WithEasing(Easing)` | On `HeroTransition`. Clip easing. Default `Easing.CubicOut`. |
| `.WithoutChromeFade()` | On `HeroTransition`. Skip fading non-hero chrome on this flight. Dest Motion chrome can rest at `TranslationX` 0. |
| `.Merge(other)` | On `HeroTransition`. Combines two flights. A later duration or easing replaces the default. Hero layers append. |

The flight already scales each tagged view to the other frame. The anchor is the origin of that scale.

Hero flights fade non-hero chrome by default after the shared-element clip is more than halfway through. Views with `TranslationX` / `TranslationY` ≠ 0 are still skipped.
