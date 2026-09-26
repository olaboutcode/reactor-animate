using Reactor.Animate.Page;

namespace Reactor.Animate.Animation;

internal readonly record struct FlipExtras(
    double AnchorX,
    double AnchorY,
    double Rotation,
    double TranslationX,
    double TranslationY)
{
    public FlipExtras Negate()
        => this with
        {
            Rotation = -Rotation,
            TranslationX = -TranslationX,
            TranslationY = -TranslationY,
        };

    public static FlipExtras Merge(FlipExtras left, FlipExtras right)
        => new(
            AnchorX: right.AnchorX != 0 || right.AnchorY != 0 ? right.AnchorX : left.AnchorX,
            AnchorY: right.AnchorX != 0 || right.AnchorY != 0 ? right.AnchorY : left.AnchorY,
            Rotation: right.Rotation != 0 ? right.Rotation : left.Rotation,
            TranslationX: right.TranslationX != 0 ? right.TranslationX : left.TranslationX,
            TranslationY: right.TranslationY != 0 ? right.TranslationY : left.TranslationY);
}

/// <summary>
/// A page transition. Concrete recipes (for example hero) merge
/// with <c>|</c>. Timing is shared; each subtype owns how it combines.
/// </summary>
public abstract class Transition
{
    public static Transition None { get; } = new NoneTransition();

    protected Transition()
        : this(Timing.PageDuration, Timing.PageEasing)
    {
    }

    private protected Transition(
        uint duration,
        Easing easing,
        FlipExtras extras = default,
        bool fadeChrome = true)
    {
        Duration = duration;
        Easing = easing;
        Extras = extras;
        FadeChrome = fadeChrome;
    }

    public uint Duration { get; }

    public Easing Easing { get; }

    public virtual IReadOnlyList<string> Tags => [];

    internal FlipExtras Extras { get; }

    internal bool FadeChrome { get; }

    internal virtual FlipExtras ExtrasFor(string tag) => Extras;

    public Transition WithDuration(uint milliseconds) => Clone(milliseconds, Easing, Extras, FadeChrome);

    public Transition WithEasing(Easing easing) => Clone(Duration, easing, Extras, FadeChrome);

    /// <summary>
    /// Do not fade non-hero chrome on this flight. Dest Motion chrome can rest
    /// at TranslationX 0. Default still fades. TranslationX/Y skip remains.
    /// </summary>
    public Transition WithoutChromeFade()
        => Clone(Duration, Easing, Extras, fadeChrome: false);

    /// <summary>
    /// Origin for scale and rotation. <c>(0, 0)</c> is top-left, <c>(0.5, 0.5)</c> is center.
    /// </summary>
    public Transition Anchor(double x, double y)
        => Clone(Duration, Easing, Extras with { AnchorX = x, AnchorY = y }, FadeChrome);

    /// <summary>
    /// Origin for scale and rotation, centered.
    /// </summary>
    public Transition AnchorCenter()
        => Clone(Duration, Easing, Extras with { AnchorX = 0.5, AnchorY = 0.5 }, FadeChrome);

    /// <summary>
    /// Origin for scale and rotation at the top-left corner.
    /// </summary>
    public Transition AnchorTopLeft()
        => Clone(Duration, Easing, Extras with { AnchorX = 0, AnchorY = 0 }, FadeChrome);

    public Transition AnchorTopRight()
        => Clone(Duration, Easing, Extras with { AnchorX = 1, AnchorY = 0 }, FadeChrome);

    public Transition AnchorBottomLeft()
        => Clone(Duration, Easing, Extras with { AnchorX = 0, AnchorY = 1 }, FadeChrome);

    public Transition AnchorBottomRight()
        => Clone(Duration, Easing, Extras with { AnchorX = 1, AnchorY = 1 }, FadeChrome);

    /// <summary>
    /// Include rotation in the FLIP invert, in degrees, then play back to rest.
    /// Pop uses the negated angle.
    /// </summary>
    public Transition Rotate(double degrees)
        => Clone(Duration, Easing, Extras with { Rotation = degrees }, FadeChrome);

    /// <summary>
    /// Extra translation added to the FLIP invert, in device-independent pixels.
    /// Pop uses the negated offset.
    /// </summary>
    public Transition Translate(double x, double y)
        => Clone(Duration, Easing, Extras with { TranslationX = x, TranslationY = y }, FadeChrome);

    internal Transition WithExtras(FlipExtras extras)
        => Clone(Duration, Easing, extras, FadeChrome);

    public abstract Transition Merge(Transition other);

    public static Transition operator |(Transition left, Transition right) => left.Merge(right);

    private protected abstract Transition Clone(
        uint duration,
        Easing easing,
        FlipExtras extras,
        bool fadeChrome);

    protected static bool MergeFadeChrome(bool left, bool right)
        => left && right;

    protected static uint MergeDuration(uint left, uint right)
        => right != Timing.PageDuration ? right : left;

    protected static Easing MergeEasing(Easing left, Easing right)
        => !ReferenceEquals(right, Timing.PageEasing) ? right : left;
}

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

public static class TransitionExtensions
{
    public static Transition Hero(this Transition transition, params string[] tags)
    {
        ArgumentNullException.ThrowIfNull(transition);
        return transition.Merge(new Hero(tags));
    }

    public static Transition Hero(this Transition transition, string tag, Func<Transition, Transition> configure)
    {
        ArgumentNullException.ThrowIfNull(transition);
        ArgumentNullException.ThrowIfNull(configure);
        return transition.Merge(configure(new Hero(tag)));
    }
}
