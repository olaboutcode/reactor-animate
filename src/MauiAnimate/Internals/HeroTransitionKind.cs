using Reactor.Animate;

namespace Reactor.Animate.Internals;

/// <summary>
/// Push or pop for an <see cref="Animate.Page"/> shared-element flight.
/// </summary>
internal enum HeroTransitionKind
{
    /// <summary>Shared-element flight while a page is pushed.</summary>
    Push,

    /// <summary>Reverse flight while a page is popped.</summary>
    Pop,
}
