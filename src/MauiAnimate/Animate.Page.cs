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
            Func<Transition, Transition>? transitionFactory = null)
            where TPage : Component, new()
            => Nav.PushAsync<TPage>(transitionFactory?.Invoke(Transition.None) ?? Transition.None);

        public static Task<MauiPage> PushAsync<TPage, TProps>(
            Func<Transition, Transition> transitionFactory,
            Action<TProps> props)
            where TPage : Component, new()
            where TProps : class, new()
            => Nav.PushAsync<TPage, TProps>(transitionFactory.Invoke(Transition.None), props);

        public static Task PopAsync() => Nav.PopAsync();
    }
}
