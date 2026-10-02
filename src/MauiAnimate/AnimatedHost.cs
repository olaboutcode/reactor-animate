using MauiReactor;

namespace Reactor.Animate;

/// <summary>
/// App-level host. Prefer <see cref="AnimatedHostExtensions.AnimateHost"/>. Renders a
/// <see cref="MauiReactor.NavigationPage"/> so <see cref="Animate.Page"/> can
/// suppress platform transitions and play shared-element clips.
/// </summary>
public class AnimatedHost : Component
{
    /// <inheritdoc/>
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

/// <summary>Wraps a MauiReactor tree in <see cref="AnimatedHost"/>.</summary>
public static class AnimatedHostExtensions
{
    /// <summary>
    /// Hosts <paramref name="node"/> in a navigation page so
    /// <see cref="Animate.Page"/> can play shared-element flights.
    /// Call this once, around the root page.
    /// </summary>
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
