using MauiReactor;
using MauiPage = Microsoft.Maui.Controls.Page;

namespace Reactor.Animate;

public static partial class Animate
{
    /// <summary>
    /// Page navigation: shared-element push/pop instead of the platform slide.
    /// </summary>
    public static partial class Page
    {
        public static Task<MauiPage> PushAsync<TPage>(
            INavigation? navigation,
            Transition? transition = null)
            where TPage : Component, new()
            => Nav.PushAsync<TPage>(navigation, transition ?? Transition.None);

        public static Task<MauiPage> PushAsync<TPage, TProps>(
            INavigation? navigation,
            Transition transition,
            Action<TProps> props)
            where TPage : Component, new()
            where TProps : class, new()
            => Nav.PushAsync<TPage, TProps>(navigation, transition, props);

        public static Task PopAsync(INavigation? navigation)
            => Nav.PopAsync(navigation);
    }
}
