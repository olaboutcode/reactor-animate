namespace Reactor.Animate;

/// <summary>
/// A page transition. Combine recipes with <see cref="Merge"/>.
/// Timing is shared; each subtype owns how it combines.
/// </summary>
public abstract class Transition
{
    /// <summary>
    /// No shared elements. 400 ms, <see cref="Easing.CubicOut"/>.
    /// </summary>
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

    /// <summary>Flight length in milliseconds. Default is 400.</summary>
    public uint Duration { get; }

    /// <summary>Flight curve. Default is <see cref="Easing.CubicOut"/>.</summary>
    public Easing Easing { get; }

    /// <summary>Shared-element tags on this transition. Empty when there are none.</summary>
    public virtual IReadOnlyList<string> Tags => [];

    internal FlipExtras Extras { get; }

    internal bool FadeChrome { get; }

    internal virtual FlipExtras ExtrasFor(string tag) => Extras;

    /// <summary>Sets the flight length in milliseconds.</summary>
    public Transition WithDuration(uint milliseconds) => Clone(milliseconds, Easing, Extras, FadeChrome);

    /// <summary>Replaces the flight curve.</summary>
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

    /// <summary>Origin for scale and rotation at the top-right corner, <c>(1, 0)</c>.</summary>
    public Transition AnchorTopRight()
        => Clone(Duration, Easing, Extras with { AnchorX = 1, AnchorY = 0 }, FadeChrome);

    /// <summary>Origin for scale and rotation at the bottom-left corner, <c>(0, 1)</c>.</summary>
    public Transition AnchorBottomLeft()
        => Clone(Duration, Easing, Extras with { AnchorX = 0, AnchorY = 1 }, FadeChrome);

    /// <summary>Origin for scale and rotation at the bottom-right corner, <c>(1, 1)</c>.</summary>
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

    /// <summary>
    /// Combines this recipe with <paramref name="other"/>. A later duration or easing
    /// replaces the default. Hero layers append.
    /// </summary>
    public abstract Transition Merge(Transition other);

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

/// <summary>Adds shared-element heroes to a page transition.</summary>
public static class TransitionExtensions
{
    /// <summary>
    /// Flies the views tagged with <paramref name="tags"/>.
    /// </summary>
    public static Transition Hero(this Transition transition, params string[] tags)
    {
        ArgumentNullException.ThrowIfNull(transition);
        return transition.Merge(new HeroTransition(tags));
    }

    /// <summary>
    /// Flies the view tagged with <paramref name="tag"/>, then applies
    /// <paramref name="configure"/> to that hero (anchor, rotation, or translation).
    /// </summary>
    public static Transition Hero(this Transition transition, string tag, Func<Transition, Transition> configure)
    {
        ArgumentNullException.ThrowIfNull(transition);
        ArgumentNullException.ThrowIfNull(configure);
        return transition.Merge(configure(new HeroTransition(tag)));
    }
}
