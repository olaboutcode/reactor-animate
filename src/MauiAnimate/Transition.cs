namespace Reactor.Animate;

/// <summary>
/// A page transition. Concrete recipes (for example <see cref="Hero"/>) merge
/// with <c>|</c>. Timing is shared; each subtype owns how it combines.
/// </summary>
public abstract class Transition(uint duration, Easing easing)
{
    public static Transition None { get; } = new NoneTransition();

    protected Transition()
        : this(Motion.DefaultDuration, Motion.DefaultEasing)
    {
    }

    public uint Duration { get; } = duration;

    public Easing Easing { get; } = easing;

    public virtual IReadOnlyList<string> Tags => [];

    public Transition WithDuration(uint milliseconds) => Clone(milliseconds, Easing);

    public Transition WithEasing(Easing easing) => Clone(Duration, easing);

    public abstract Transition Merge(Transition other);

    public static Transition operator |(Transition left, Transition right) => left.Merge(right);

    protected abstract Transition Clone(uint duration, Easing easing);

    protected static uint MergeDuration(uint left, uint right)
        => right != Motion.DefaultDuration ? right : left;

    protected static Easing MergeEasing(Easing left, Easing right)
        => !ReferenceEquals(right, Motion.DefaultEasing) ? right : left;
}

sealed class NoneTransition : Transition
{
    public NoneTransition()
    {
    }

    public NoneTransition(uint duration, Easing easing)
        : base(duration, easing)
    {
    }

    public override Transition Merge(Transition other)
    {
        ArgumentNullException.ThrowIfNull(other);

        var duration = MergeDuration(Duration, other.Duration);
        var easing = MergeEasing(Easing, other.Easing);
        var merged = other;
        if (duration != other.Duration)
            merged = merged.WithDuration(duration);
        if (!ReferenceEquals(easing, other.Easing))
            merged = merged.WithEasing(easing);
        return merged;
    }

    protected override Transition Clone(uint duration, Easing easing)
        => new NoneTransition(duration, easing);
}

public static class TransitionExtensions
{
    public static Transition Hero(this Transition transition, params string[] tags)
    {
        ArgumentNullException.ThrowIfNull(transition);
        return transition.Merge(new Hero(tags));
    }
}
