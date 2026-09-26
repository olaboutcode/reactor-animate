using MauiReactor;
using MauiPage = Microsoft.Maui.Controls.Page;
using Reactor.Animate.Page;

namespace Reactor.Animate;

public static partial class Animate
{
    /// <summary>
    /// Page navigation: shared-element push/pop instead of the platform slide.
    /// </summary>
    public static partial class Page
    {
        static EventHandler<HeroTransitionEventArgs>? _heroStarted;
        static EventHandler<HeroTransitionEventArgs>? _heroInFlight;
        static EventHandler<HeroTransitionEventArgs>? _heroEnded;

        /// <summary>
        /// Page is laid out and the hold frame still covers the window. Invert
        /// has not run: hero layout changes here are picked up. The same
        /// handler instance is only stored once.
        /// </summary>
        public static event EventHandler<HeroTransitionEventArgs>? HeroStarted
        {
            add => Add(ref _heroStarted, value);
            remove => _heroStarted -= value;
        }

        /// <summary>
        /// Hold frame is gone and the shared-element clip is on screen.
        /// Layout, visibility, and tag unregistration on flying heroes are
        /// ignored until <see cref="HeroEnded"/>. Raised once per flight.
        /// Follow progress with <see cref="HeroTransitionEventArgs.At"/>.
        /// </summary>
        public static event EventHandler<HeroTransitionEventArgs>? HeroInFlight
        {
            add => Add(ref _heroInFlight, value);
            remove => _heroInFlight -= value;
        }

        /// <summary>
        /// Clip finished (or there was nothing to play). Always raised after
        /// <see cref="HeroStarted"/>, even when a handler throws.
        /// </summary>
        public static event EventHandler<HeroTransitionEventArgs>? HeroEnded
        {
            add => Add(ref _heroEnded, value);
            remove => _heroEnded -= value;
        }

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

        internal static void RaiseHeroStarted(HeroTransitionEventArgs args)
            => Raise(_heroStarted, args);

        internal static void RaiseHeroInFlight(HeroTransitionEventArgs args)
            => Raise(_heroInFlight, args);

        internal static void RaiseHeroEnded(HeroTransitionEventArgs args)
            => Raise(_heroEnded, args);

        static void Add(
            ref EventHandler<HeroTransitionEventArgs>? field,
            EventHandler<HeroTransitionEventArgs>? value)
        {
            if (value is null)
                return;
            field -= value;
            field += value;
        }

        static void Raise(EventHandler<HeroTransitionEventArgs>? handlers, HeroTransitionEventArgs args)
        {
            if (handlers is null)
                return;

            foreach (var candidate in handlers.GetInvocationList())
            {
                try
                {
                    ((EventHandler<HeroTransitionEventArgs>)candidate)(null, args);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }
    }
}
