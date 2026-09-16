using MauiPage = Microsoft.Maui.Controls.Page;

namespace Reactor.Animate;

/// <summary>
/// Push or pop for an <see cref="Animate.Page"/> shared-element flight.
/// </summary>
public enum HeroTransitionKind
{
    Push,
    Pop,
}

/// <summary>
/// Payload for <see cref="Animate.Page.HeroStarted"/>, <see cref="Animate.Page.HeroInFlight"/>,
/// and <see cref="Animate.Page.HeroEnded"/>.
/// </summary>
public sealed class HeroTransitionEventArgs(
    HeroTransitionKind kind,
    Transition transition,
    MauiPage page) : EventArgs
{
    public HeroTransitionKind Kind { get; } = kind;

    public Transition Transition { get; } = transition ?? throw new ArgumentNullException(nameof(transition));

    /// <summary>
    /// Destination page on push; the page being revealed on pop.
    /// </summary>
    public MauiPage Page { get; } = page ?? throw new ArgumentNullException(nameof(page));

    public IReadOnlyList<string> Tags => Transition.Tags;
}
