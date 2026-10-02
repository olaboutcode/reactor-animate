using Reactor.Animate;

namespace Reactor.Animate.Internals;

internal sealed class NoneTransition : Transition
{
    public NoneTransition()
    {
    }

    public NoneTransition(uint duration, Easing easing, FlipExtras extras = default, bool fadeChrome = true)
        : base(duration, easing, extras, fadeChrome)
    {
    }

    public override Transition Merge(Transition other)
    {
        ArgumentNullException.ThrowIfNull(other);

        var duration = MergeDuration(Duration, other.Duration);
        var easing = MergeEasing(Easing, other.Easing);
        var extras = FlipExtras.Merge(Extras, other.Extras);
        var merged = other;
        if (duration != other.Duration)
            merged = merged.WithDuration(duration);
        if (!ReferenceEquals(easing, other.Easing))
            merged = merged.WithEasing(easing);
        if (extras != merged.Extras)
            merged = merged.WithExtras(extras);
        if (!MergeFadeChrome(FadeChrome, other.FadeChrome))
            merged = merged.WithoutChromeFade();
        return merged;
    }

    private protected override Transition Clone(uint duration, Easing easing, FlipExtras extras, bool fadeChrome)
        => new NoneTransition(duration, easing, extras, fadeChrome);
}
