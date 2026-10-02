# Composition

Stagger, keyframes, timelines, repeat, springs, and path are layers on one player.

| Member | Role |
|---|---|
| `.Stagger(step, from, grid?)` | Delay each bound target on linear player time (`StaggerFrom.Start` / `Center` / `End`). |
| `.Keyframes` / `.Opacity(k => k.At(…))` | 0–1 offsets of this motion. |
| `.Add(child, at, id?)` / `.Then(next, id?)` | Timeline. `Then` starts at the current span. `TrySpan` / `Seek(id)` use the name. |
| `.And(other)` | Plays the other recipe at the same time. The parent span is the longer one. Tracks keep their millisecond length. |
| `.Repeat(n)` / `.Yoyo()` | `Repeat(1)` is once; `Repeat(-1)` until Pause, Reset, or Dispose. A yoyo cycle is forward then reverse. |
| `.WithSpring(Spring)` | Mass-spring-damper until rest (`Spring.Default` / `Snappy` / `Gentle`). Duration becomes “until rest.” Do not combine with `WithDuration` / `Stagger`. |
| `.Path(geometry)` | TranslationX/Y along a `PathGeometry` (line, cubic, `ArcSegment`). |

```csharp
Animate.Motion.Play(
    m => m.FadeIn().Scale(0.9, 1).Stagger(40, StaggerFrom.Start, (3, 4)).WithDuration(280),
    tiles);

.Add(fade, at: 0, id: "intro").Then(pulse, id: "pulse");
player.Seek("pulse");

.Scale(1, 1.16).WithSpring(Spring.Snappy);

.Path(geometry).WithDuration(900).WithEasing(Easing.SinInOut);
```
