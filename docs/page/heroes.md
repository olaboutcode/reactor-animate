# Heroes

```csharp
BoxView().Hero("cover")
Button("Open", Open).Hero("button")
```

| Member | Role |
|---|---|
| `VisualNode.Hero(string tag)` | Marks a view as a shared element. The tag must match on source and destination. |

Tags are ordinal strings. A view may have one tag. Several pairs may fly in the same push, one pair per shared tag.

`.Hero` attaches to any `VisualElement`. FLIP morphs the frame (position and size) plus compatible properties such as color and corner radius. Same `Image.Source` and similar `Aspect` look like a growing or shrinking photo. Different sources cut; they do not cross-fade pixels. Internal fit (`AspectFit` vs `AspectFill`) can jump on the last frame.

`Hero()` wraps the tagged node in a `Grid` and registers the first visual child as the hero target.

## Several heroes, per-tag extras

```csharp
Animate.Page.PushAsync<DetailPage>(ht => ht
    .Hero("cover", h => h.AnchorCenter())
    .Hero("from_tl", h => h.AnchorTopLeft())
    .Hero("spin_90", h => h.AnchorCenter().Rotate(90))
    .Hero(["c", "d"], h => h.AnchorCenter())
    .WithEasing(Easing.CubicInOut));
```

`ht` is a `HeroTransition`. `h` is a `Hero`. Several tags can share one `Hero` by passing the list before the callback.
