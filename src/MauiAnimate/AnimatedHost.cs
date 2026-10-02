using MauiReactor;

namespace Reactor.Animate;

/// <summary>
/// App-level host. Prefer <see cref="VisualNodeExtensions.AnimateHost"/>. Renders a
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
