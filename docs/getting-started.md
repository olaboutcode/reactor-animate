# Getting started

```
dotnet add package Reactor.Animate --prerelease
```

Wrap the root page, tag matching views, and push through `Animate.Page`.

```csharp
class App : Component
{
    public override VisualNode Render()
        => new HomePage().AnimateHost();
}

class HomePage : Component
{
    public override VisualNode Render()
        => ContentPage(
            BoxView()
                .HeightRequest(72)
                .CornerRadius(12)
                .BackgroundColor(Colors.OrangeRed)
                .OnTapped(Open)
                .Hero("cover")
        )
        .HasNavigationBar(false);

    static Task Open()
        => Animate.Page.PushAsync<DetailPage>(t => t
            .Hero("cover", h => h.AnchorCenter())
            .WithDuration(400)
            .WithEasing(Easing.CubicOut));
}

class DetailPage : Component
{
    public override VisualNode Render()
        => ContentPage(
            VStack(
                BoxView()
                    .HeightRequest(180)
                    .CornerRadius(24)
                    .BackgroundColor(Colors.OrangeRed)
                    .Hero("cover"),
                Button("Back", async () => await Animate.Page.PopAsync())
            )
        )
        .HasNavigationBar(false);
}
```

`PopAsync` is a reverse hero on the source page: hold dest, pop, invert source heroes to dest frames with **negated extras**, then play to rest. `Rotate(90)` on push becomes `-90` on pop; `Translate(50, 0)` becomes `(-50, 0)`. The host intercepts Android system back and disables the iOS edge-swipe so a platform pop cannot run over the clip.

Chain `.Hero()` after tap handlers: `Hero()` returns `VisualNode`, so `OnTapped` must run on the control first.

Defaults: **400 ms**, **`Easing.CubicOut`**.

Everything lives in `Reactor.Animate`.
