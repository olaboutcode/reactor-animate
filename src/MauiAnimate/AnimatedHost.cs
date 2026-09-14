using MauiReactor;

namespace Reactor.Animate;

/// <summary>
/// App-level host. Prefer <see cref="AnimatedHostExtensions.Host"/>. Renders a
/// <see cref="MauiReactor.NavigationPage"/> so <see cref="Animate.Page"/> can
/// suppress platform transitions and play shared-element clips.
/// </summary>
public class AnimatedHost : Component
{
    public override VisualNode Render()
        => NavigationPage(page =>
            {
                if (page is null)
                    return;

                HostContext.Current.Navigation = page.Navigation;
                page.ChildAdded -= HideIncomingPage;
                page.ChildAdded += HideIncomingPage;
                page.Pushed -= HideIncoming;
                page.Pushed += HideIncoming;
            }, Children())
            .OnUnloaded(() => HostContext.Current.Navigation = null);

    static void HideIncoming(object? sender, NavigationEventArgs e)
        => PageCover.Conceal(e.Page);

    static void HideIncomingPage(object? sender, ElementEventArgs e)
    {
        if (e.Element is Microsoft.Maui.Controls.Page page
            && sender is Microsoft.Maui.Controls.NavigationPage nav
            && nav.Navigation.NavigationStack.Count > 1)
            PageCover.Conceal(page);
    }
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
