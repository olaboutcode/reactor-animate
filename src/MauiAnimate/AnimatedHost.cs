using MauiReactor;

namespace Reactor.Animate;

/// <summary>
/// App-level host. Prefer <see cref="AnimatedHostExtensions.Host"/>. Renders a
/// <see cref="MauiReactor.NavigationPage"/> so shared-element clips can replace
/// the platform slide.
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
