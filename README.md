# Reactor.Animate

[![CI](https://github.com/olaboutcode/reactor-animate/actions/workflows/ci.yml/badge.svg)](https://github.com/olaboutcode/reactor-animate/actions/workflows/ci.yml)

Shared-element page transitions and in-page view motion for [MauiReactor](https://github.com/adospace/reactorui-maui).

| | |
|:---|:---|
| Package | `Reactor.Animate` `0.1.0-alpha` |
| Namespace | `Reactor.Animate` · `Reactor.Animate.Animation` |
| Targets | .NET 10 · MAUI 10 · Android · iOS · Mac Catalyst |
| License | MIT |

This is not a Shell replacement, not a router, and not a port of FluidNav. MauiReactor still owns the navigation stack.

## Table of contents

- [About](#about)
- [Getting started](#getting-started)
- [Documentation](#documentation)
- [Sample](#sample)
- [Building](#building)
- [License](#license)

## About

Tag matching views on two pages. Push and pop play a shared-element clip instead of the platform slide. In-page motion uses `Animate.Motion` (reusable recipes, play / reverse / pause / reset) or MauiReactor `WithAnimation` (state morphs).

- **`Animate.Page`** — shared-element flights (`.Hero`, `PushAsync` / `PopAsync`).
- **`Animate.Motion`** — in-page clips (bind a recipe, then play / reverse / seek).

Hero flights and Motion compose. Fade, slide, and scale recipes live on Motion, not on Page.

## Getting started

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

## Documentation

The library reference lives in `docs/`:

- [Getting started](docs/getting-started.md)
- [Page](docs/page/host.md) — host, heroes, navigation, transitions, events, flights
- [Motion](docs/motion/overview.md) — recipes, playback, path, stagger, springs
- [Samples](docs/samples.md)

## Sample

`samples/Sample` is a MauiReactor gallery: home heroes, CollectionView tiles, Photos (Image-to-Image), Circle chrome, Motion playground, stagger, scrub, color spaces, timeline seek, and path (Bézier + arc).

## Building

```
dotnet test tests/MauiAnimate.Tests/MauiAnimate.Tests.csproj
```

CI runs tests on Ubuntu (`net10.0`) and builds `net10.0-android` and `net10.0-ios`.

Preview the docs locally:

```
pip install -r docs/requirements.txt
mkdocs serve
```

## License

MIT
