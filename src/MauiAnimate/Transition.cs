namespace Reactor.Animate;

readonly record struct MotionExtras(
    double AnchorX,
    double AnchorY,
    double Rotation,
    bool Translate)
{
    public static MotionExtras Merge(MotionExtras left, MotionExtras right)
        => new(
            AnchorX: right.AnchorX != 0 || right.AnchorY != 0 ? right.AnchorX : left.AnchorX,
            AnchorY: right.AnchorX != 0 || right.AnchorY != 0 ? right.AnchorY : left.AnchorY,
            Rotation: right.Rotation != 0 ? right.Rotation : left.Rotation,
            Translate: left.Translate || right.Translate);
}

/// <summary>
/// A page transition. Concrete recipes (for example <see cref="Hero"/>) merge
/// with <c>|</c>. Timing is shared; each subtype owns how it combines.
/// </summary>
public abstract class Transition
{
    public static Transition None { get; } = new NoneTransition();

    protected Transition()
        : this(Motion.DefaultDuration, Motion.DefaultEasing)
    {
    }

    private protected Transition(uint duration, Easing easing, MotionExtras extras = default)
    {
        Duration = duration;
        Easing = easing;
        Extras = extras;
    }

    public uint Duration { get; }

    public Easing Easing { get; }

    public virtual IReadOnlyList<string> Tags => [];

    internal MotionExtras Extras { get; }

    public Transition WithDuration(uint milliseconds) => Clone(milliseconds, Easing, Extras);

    public Transition WithEasing(Easing easing) => Clone(Duration, easing, Extras);

    /// <summary>
    /// Origin for scale and rotation. <c>(0, 0)</c> is top-left, <c>(0.5, 0.5)</c> is center.
    /// </summary>
    public Transition Anchor(double x, double y)
        => Clone(Duration, Easing, Extras with { AnchorX = x, AnchorY = y });

    /// <summary>
    /// Origin for scale and rotation, centered.
    /// </summary>
    public Transition AnchorCenter()
        => Clone(Duration, Easing, Extras with { AnchorX = 0.5, AnchorY = 0.5 });

    /// <summary>
    /// Include rotation in the FLIP invert, in degrees, then play back to rest.
    /// </summary>
    public Transition Rotate(double degrees)
        => Clone(Duration, Easing, Extras with { Rotation = degrees });

    /// <summary>
    /// Include translation in the FLIP. The delta is measured from First/Last, not passed in.
    /// </summary>
    public Transition Translate()
        => Clone(Duration, Easing, Extras with { Translate = true });

    internal Transition WithExtras(MotionExtras extras)
        => Clone(Duration, Easing, extras);

    public abstract Transition Merge(Transition other);

    public static Transition operator |(Transition left, Transition right) => left.Merge(right);

    private protected abstract Transition Clone(uint duration, Easing easing, MotionExtras extras);

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

    public NoneTransition(uint duration, Easing easing, MotionExtras extras = default)
        : base(duration, easing, extras)
    {
    }

    public override Transition Merge(Transition other)
    {
        ArgumentNullException.ThrowIfNull(other);

        var duration = MergeDuration(Duration, other.Duration);
        var easing = MergeEasing(Easing, other.Easing);
        var extras = MotionExtras.Merge(Extras, other.Extras);
        var merged = other;
        if (duration != other.Duration)
            merged = merged.WithDuration(duration);
        if (!ReferenceEquals(easing, other.Easing))
            merged = merged.WithEasing(easing);
        if (extras != merged.Extras)
            merged = merged.WithExtras(extras);
        return merged;
    }

    private protected override Transition Clone(uint duration, Easing easing, MotionExtras extras)
        => new NoneTransition(duration, easing, extras);
}

public static class TransitionExtensions
{
    public static Transition Hero(this Transition transition, params string[] tags)
    {
        ArgumentNullException.ThrowIfNull(transition);
        return transition.Merge(new Hero(tags));
    }
}
