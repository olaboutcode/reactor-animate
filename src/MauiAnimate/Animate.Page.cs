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

        /// <summary>
        /// Pushes <typeparamref name="TPage"/> with no platform animation and plays
        /// <paramref name="transitionFactory"/>. The factory receives an empty <see cref="HeroTransition"/>.
        /// Omit it when nothing is shared.
        /// A push that starts while a flight is running returns the current page.
        /// </summary>
        /// <returns>The destination page, or the current page when a flight is already running.</returns>
        public static Task<MauiPage> PushAsync<TPage>(
            Func<HeroTransition, HeroTransition>? transitionFactory = null)
            where TPage : Component, new()
            => HeroNavigation.PushAsync<TPage>(transitionFactory?.Invoke(HeroTransition.Empty) ?? HeroTransition.Empty);

        /// <summary>
        /// Pushes <typeparamref name="TPage"/> with <typeparamref name="TProps"/> and plays
        /// <paramref name="transitionFactory"/>. The factory receives an empty <see cref="HeroTransition"/>.
        /// A push that starts while a flight is running returns the current page.
        /// </summary>
        /// <returns>The destination page, or the current page when a flight is already running.</returns>
        public static Task<MauiPage> PushAsync<TPage, TProps>(
            Func<HeroTransition, HeroTransition> transitionFactory,
            Action<TProps> props)
            where TPage : Component, new()
            where TProps : class, new()
            => HeroNavigation.PushAsync<TPage, TProps>(transitionFactory.Invoke(HeroTransition.Empty), props);

        /// <summary>
        /// Plays the reverse flight, then pops. Returns immediately when a flight
        /// is already running or the stack has one page.
        /// </summary>
        public static Task PopAsync() => HeroNavigation.PopAsync();

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
