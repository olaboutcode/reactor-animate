# Reactor.Animate

An animation library for [MauiReactor](https://github.com/adospace/reactorui-maui). It is inspired by MauiReactor's own property and animation controller, flutter, animejs, and pulls some elements from [beto-rodrigez](https://github.com/beto-rodriguez)'s [FluidNav](https://github.com/beto-rodriguez/FluidNav).

> [!WARNING]
> The library is still experimental, use at your own risk. The performance seems decent and all the features should be working as expected. However, expect breaking changes as I work on the API.

### Table of contents

- [Getting started](#getting-started)
- [Documentation](#documentation)
- [Samples](#sample)
- [Building](#building)
- [License](#license)

### Getting started

Reactor.Animate is meant to work for MauiReactor app
1. Follow [MauiReactor instructions](https://github.com/adospace/reactorui-maui#setting-up-mauireactor-from-cli) on how to create a MauiReactor app.
2. Install Reactor.Animate
```
dotnet add package Reactor.Animate --prerelease
```
##### Examples
1. For page Hero animations, wrap the root page, tag matching views, and push through `Animate.Page`.

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
                .BackgroundColor(Colors.OrangeRed)
                .OnTapped(Open)
                .Hero("cover")
        )
        .HasNavigationBar(false);

    static Task Open()
        => Animate.Page.PushAsync<DetailPage>(t => t
            .Hero("cover", h => h.AnchorCenter())
            .WithEasing(Easing.CubicOut));
}

class DetailPage : Component
{
    public override VisualNode Render()
        => ContentPage(
            VStack(
                BoxView()
                    .HeightRequest(180)
                    .BackgroundColor(Colors.OrangeRed)
                    .Hero("cover"),
                Button("Back", async () => await Animate.Page.PopAsync())
            )
        )
        .HasNavigationBar(false);
}
```

2. For in-page animation, animate elements using `Animate.Motion`
```csharp
sealed class MotionAnimation : Component
{
    private MotionPlayer? _player;

    public override VisualNode Render()
        => ContentPage("Counter Sample",
            VStack(
                Label($"This is awesome!")
                    .BindMotion(
                        Animate.Motion.Define(
                            m => m
                                .RotateX(180)
                                .WithDuration(500)
                                .WithEasing(Easing.CubicInOut)
                        ),
                        p => _player = p
                    ),

                HStack(
                    Button("Play Text Animation")
                        .OnClicked(async () => await _player.ForwardAsync()),
                    Button("Reverse Text Animation")
                        .OnClicked(async () => await _player.ReverseAsync())
                )
                .Spacing(10)
            )
            .Spacing(30)
            .Center()
        );
}
```

### Documentation

The library reference lives in `docs/`:

- [Getting started](docs/getting-started.md)
- [Page](docs/page/host.md) — host, heroes, navigation, transitions, events, flights
- [Motion](docs/motion/overview.md) — recipes, playback, path, stagger, springs
- [Samples](docs/samples.md)

### Sample

`samples/Sample` is a MauiReactor gallery: home heroes, CollectionView tiles, Photos (Image-to-Image), Circle chrome, Motion playground, stagger, scrub, color spaces, timeline seek, and path (Bézier + arc).

### Building

```
dotnet test tests/MauiAnimate.Tests/MauiAnimate.Tests.csproj
```

CI runs tests on Ubuntu (`net10.0`) and builds `net10.0-android` and `net10.0-ios`.

### License

MIT
