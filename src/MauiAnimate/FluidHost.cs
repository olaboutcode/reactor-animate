using MauiReactor;

namespace Reactor.Animate;

/// <summary>
/// App-level host. Wrap the root page; it renders a <see cref="MauiReactor.NavigationPage"/>
/// so <see cref="Nav"/> can suppress platform transitions and play clips.
/// </summary>
public class FluidHost : Component
{
    public override VisualNode Render()
        => NavigationPage(Children());
}
