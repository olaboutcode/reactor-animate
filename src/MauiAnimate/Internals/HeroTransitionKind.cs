using Reactor.Animate;

namespace Reactor.Animate.Internals;

/// <summary>
/// Whether the flight <see cref="HeroNavigation"/> is playing is a push or a pop.
/// The public event args expose this as <c>IsPushTransition</c> and <c>IsPopTransition</c>.
/// A pop builds a new clip from negated extras. It does not reverse the push clip.
/// </summary>
internal enum HeroTransitionKind
{
    /// <summary>Shared-element flight while a page is pushed.</summary>
    Push,

    /// <summary>Reverse flight while a page is popped.</summary>
    Pop,
}
