# Reactor.Animate

[![NuGet](https://img.shields.io/nuget/vpre/Reactor.Animate.svg?label=nuget)](https://www.nuget.org/packages/Reactor.Animate)

Shared-element page transitions and in-page view motion for [MauiReactor](https://github.com/adospace/reactorui-maui).

Tag matching views on two pages. Push and pop play a shared-element clip instead of the platform slide. In-page motion uses `Animate.Motion` (reusable recipes, play / reverse / pause / reset) or MauiReactor `WithAnimation` (state morphs).

| | |
|:---|:---|
| Package | `Reactor.Animate` `0.1.0-alpha.2` |
| Namespace | `Reactor.Animate` · `Reactor.Animate.Animation` |
| Targets | .NET 10 · MAUI 10 · Android · iOS · Mac Catalyst |
| License | MIT |

This is not a Shell replacement, not a router, and not a port of FluidNav. MauiReactor still owns the navigation stack.

## Two families

- **`Animate.Page`** — shared-element flights. Tag views with `.Hero`, push and pop through `Animate.Page.PushAsync` / `PopAsync`.
- **`Animate.Motion`** — in-page clips. Bind a recipe to any view and play, reverse, pause, seek, stagger, or spring it.

Hero flights and Motion compose: the image flies, dest chrome plays from `HeroInFlight`. Fade, slide, and scale recipes live on Motion, not on Page.

## Next

- [Getting started](getting-started.md) — host, tag, push.
- [Page](page/host.md) — host, heroes, navigation, transitions, events, flights.
- [Motion](motion/overview.md) — recipes, playback, path, stagger, springs.
- [Samples](samples.md) — the MauiReactor gallery in `samples/Sample`.
