# Navigation

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

The factory is `Func<Transition, Transition>`. It receives `Transition.None`.

## One cell from a grid

```csharp
Animate.Page.PushAsync<GalleryDetailPage, GalleryItemProps>(
    t => t.Hero($"tile-{item.Id}", h => h.AnchorCenter()).WithDuration(300),
    props =>
    {
        props.Id = item.Id;
        props.Color = item.Color;
    });
```

The destination uses the same tag: `.Hero($"tile-{Props.Id}")`. Measure the source at tap time. Do not hold CollectionView cell references.
