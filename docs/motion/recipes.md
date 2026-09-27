# Recipes

Build a recipe from `Motion.None` with `Animate.Motion.Define`. Tracks write bindable properties. Named helpers (`FadeIn`, `SlideIn`, …) are the same tracks with preset from/to.

| Member | Role |
|---|---|
| `Animate.Motion.Define` | Build a recipe from `Motion.None`. |
| `.Opacity` `.Translate` `.Scale` `.Rotate` `.BackgroundColor` `.Width` `.Height` `.CornerRadius` `.Property` `.Path` | Property tracks. `Scale` writes ScaleX and ScaleY. `Path` writes TranslationX/Y along a `PathGeometry`. Colors lerp in HSV (shortest hue); `.WithColorSpace(ColorSpace.Rgb)` for channel-wise. |
| `.FadeIn` `.FadeOut` `.SlideIn` `.SlideOut` `.ScaleIn` `.ScaleOut` | Named recipes. `SlideIn` / `SlideOut` take `SlideFrom`. |
| `left \| right` | Parallel merge; parent span is `max(left, right)`. Tracks are not stretched. |

```csharp
Animate.Motion.Define(m => m
    .FadeIn()
    .Scale(0.9, 1)
    .WithDuration(280));
```

`Motion.Translate` / `Rotate` are view properties. `Transition.Translate` / `Rotate` are FLIP extras. Same method names, different types, both in `Reactor.Animate.Animation`.
