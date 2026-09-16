using MauiReactor;
using Reactor.Animate.Page;

namespace Reactor.Animate;

/// <summary>
/// App-level host. Prefer <see cref="AnimatedHostExtensions.AnimateHost"/>. Renders a
/// <see cref="MauiReactor.NavigationPage"/> so <see cref="Animate.Page"/> can
/// suppress platform transitions and play shared-element clips.
/// </summary>
public class AnimatedHost : Component
{
    public override VisualNode Render()
        => NavigationPage(page =>
            {
                HostContext.Current.Navigation = page?.Navigation;
                PlatformPop.Attach(page);
            }, Children())
            .OnUnloaded(() =>
            {
                PlatformPop.Detach();
                HostContext.Current.Navigation = null;
            });
}

public static class AnimatedHostExtensions
{
    public static AnimatedHost AnimateHost(this VisualNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var host = new AnimatedHost
        {
            node
        };
        return host;
    }
}
