# Recipes

Build a recipe from `Motion.None` with `Animate.Motion.Define`. Tracks write bindable properties. Named helpers (`FadeIn`, `SlideIn`, …) are the same tracks with preset from/to.

| Member | Role |
|---|---|
| `Animate.Motion.Define` | Build a recipe from `Motion.None`. |
| `.Opacity` `.Translate` `.Scale` `.Rotate` `.RotateX` `.RotateY` `.BackgroundColor` `.Width` `.Height` `.CornerRadius` `.Property` `.Path` | Property tracks. `Scale` writes ScaleX and ScaleY. `RotateX` / `RotateY` are degrees about the horizontal and vertical axes. `Path` writes TranslationX/Y along a `PathGeometry`. Colors lerp in HSV (shortest hue); `.WithColorSpace(ColorSpace.Rgb)` for channel-wise. |
| `.Perspective(entry)` | Eye distance `1/entry` for `RotateX` / `RotateY`, the same number as Flutter `Matrix4.setEntry(3, 2, entry)`. The far edge shrinks and the near edge grows. `0.001` is mild. `0` keeps both edges the same height. |
| `.FadeIn` `.FadeOut` `.SlideIn` `.SlideOut` `.ScaleIn` `.ScaleOut` | Named recipes. `SlideIn` / `SlideOut` take `SlideFrom`. |
| `left \| right` | Parallel merge; parent span is `max(left, right)`. Tracks are not stretched. |

```csharp
Animate.Motion.Define(m => m
    .FadeIn()
    .Scale(0.9, 1)
    .WithDuration(280));
```

`Motion.Translate` / `Rotate` are view properties. `Transition.Translate` / `Rotate` are FLIP extras. Same method names, different types, both in `Reactor.Animate`.

A plane turning in depth:

```csharp
Motion.None
    .Perspective(0.003)
    .RotateY(0, 55)
    .WithDuration(400);
```

`Perspective` is rebuilt onto the native transform on iOS and Mac Catalyst (`CATransform3D.m34`). On Android the same eye distance is passed to `CameraDistance`, scaled by `density² × √5` so the foreshortening matches that entry. Windows uses `PlaneProjection`, which shrinks the far edge at the platform distance.
