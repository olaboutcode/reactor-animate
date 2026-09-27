# Playback

| Member | Role |
|---|---|
| `Animate.Motion.Bind` / `Motion.Bind` | Bind to one or more views. Does not start. |
| `Animate.Motion.Play` | Bind and start forward. Returns the player. |
| `Animate.Motion.ForwardAsync` / `ReverseAsync` | One-shot bind and play. |
| `VisualNode.BindMotion(motion, onBind?)` | Bind on Loaded, dispose on Unloaded. Subscribe in `onBind`. |
| `Forward()` / `Reverse()` | Start. Null-safe; does not throw on cancel. |
| `ForwardAsync()` / `ReverseAsync()` | Same, awaitable. Null-safe; does not throw on cancel. |
| `Seek(ms)` | Linear wall-clock position. Leaves `Paused` for a mid-span seek. |
| `SeekFraction(t)` | Eased progress (same units as `Progress` / `At`). Leaves `Paused`; does not play. |
| `Seek(id)` / `TrySpan(id, …)` | Named timeline child, linear player time. |
| `MotionPlayer.At` | Player-long progress ticks (eased `t`; decreases on reverse). Springs report the spring `x`. |
| `Started` `Completed` `Paused` `Resumed` `StatusChanged` | Lifecycle. |

`Animate.Motion.Play` is bind + start + **keep the player** (pause or reverse later). `Animate.Motion.ForwardAsync` also starts, but you only get a Task — no player. Use `Bind` / `BindMotion` when you will call `Forward()` yourself.

## Scrub

`SeekFraction(t)` leaves the player paused. Lifting the slider does not resume; call `ForwardAsync` to play.
