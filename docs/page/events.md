# Events

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
    if (!e.IsPushTransition())
        return;
    e.At(t =>
    {
        if (t >= 0.5)
            SetState(s => s.ShowChrome = true);
    });
}

void OnHeroEnded(object? sender, HeroTransitionEventArgs e)
{
    if (!e.IsPushTransition())
        return;
    if (!e.Tags.Contains("cover"))
        return;
    // follow-up work on the live page
}
```

| Event | When | Safe to do |
|---|---|---|
| `HeroStarted` | `e.Page` is laid out. The hold still covers the window. Invert has not run. | Change page content, including hero layout. Changes are measured, then invert runs. |
| `HeroInFlight` | Hold is gone. The clip is visible. Raised once per flight, not per frame. | Work that should appear *with* the morph. Flying-hero layout is locked. Follow the curve with `e.At(t => ...)`. |
| `HeroEnded` | The clip has finished, or there was nothing to play. Always raised after `HeroStarted`, even if a handler throws. | Follow-up work on the live page. Layout is unlocked. |

`HeroTransitionEventArgs`

| Property / method | Meaning |
|---|---|
| `IsPushTransition()` | This flight is a push. |
| `IsPopTransition()` | This flight is a pop. |
| `Page` | Destination on push; the page being revealed on pop. |
| `Transition` | The `HeroTransition` that was played. |
| `Tags` | Tags on that transition. |
| `Progress` | 0–1 along the clip, using the same easing as the flight (`e.Transition.Easing`). 0 at `HeroStarted`, 1 at `HeroEnded`. |
| `At(callback)` | `Action<double>` invoked with `Progress` now and on each tick of this flight. |

The same handler instance is stored only once. A **new lambda on every tap** still stacks, because those are different delegates. Subscribe in `OnMounted` and unsubscribe in `OnWillUnmount`.

A throwing handler is logged and skipped. The flight still runs, and `HeroEnded` is still raised.

Busy flights and a one-page stack do not raise events.

Start dest Motion chrome from `HeroInFlight` (`e.At(t => …)`), not `HeroEnded`, if it should overlap the flight.
