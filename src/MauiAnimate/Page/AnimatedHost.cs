using MauiReactor;
using Reactor.Animate.Page;

namespace Reactor.Animate;

/// <summary>
/// App-level host. Prefer <see cref="AnimatedHostExtensions.Host"/>. Renders a
/// <see cref="MauiReactor.NavigationPage"/> so <see cref="Animate.Page"/> can
/// suppress platform transitions and play shared-element clips.
/// </summary>
public class AnimatedHost : Component
{
    public override VisualNode Render()
        => NavigationPage(page => HostContext.Current.Navigation = page?.Navigation, Children())
            .OnUnloaded(() => HostContext.Current.Navigation = null);
}

public static class AnimatedHostExtensions
{
    public static AnimatedHost Host(this VisualNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var host = new AnimatedHost();
        host.Add(node);
        return host;
    }
}
