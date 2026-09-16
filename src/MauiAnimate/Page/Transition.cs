using Reactor.Animate.Page;

namespace Reactor.Animate;

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
        : this(Tween.DefaultDuration, Tween.DefaultEasing)
    {
    }

    private protected Transition(
        uint duration,
        Easing easing,
        FlipExtras extras = default,
        PageRecipe page = default)
    {
        Duration = duration;
        Easing = easing;
        Extras = extras;
        PageMotion = page == default ? PageRecipe.Empty : page;
    }

    public uint Duration { get; }

    public Easing Easing { get; }

    public virtual IReadOnlyList<string> Tags => [];

    internal FlipExtras Extras { get; }

    internal PageRecipe PageMotion { get; }

    internal virtual FlipExtras ExtrasFor(string tag) => Extras;

    public Transition WithDuration(uint milliseconds) => Clone(milliseconds, Easing, Extras, PageMotion);

    public Transition WithEasing(Easing easing) => Clone(Duration, easing, Extras, PageMotion);

    /// <summary>
    /// Origin for scale and rotation. <c>(0, 0)</c> is top-left, <c>(0.5, 0.5)</c> is center.
    /// </summary>
    public Transition Anchor(double x, double y)
        => Clone(Duration, Easing, Extras with { AnchorX = x, AnchorY = y }, PageMotion);

    /// <summary>
    /// Origin for scale and rotation, centered.
    /// </summary>
    public Transition AnchorCenter()
        => Clone(Duration, Easing, Extras with { AnchorX = 0.5, AnchorY = 0.5 }, PageMotion);

    /// <summary>
    /// Origin for scale and rotation at the top-left corner.
    /// </summary>
    public Transition AnchorTopLeft()
        => Clone(Duration, Easing, Extras with { AnchorX = 0, AnchorY = 0 }, PageMotion);

    public Transition AnchorTopRight()
        => Clone(Duration, Easing, Extras with { AnchorX = 1, AnchorY = 0 }, PageMotion);

    public Transition AnchorBottomLeft()
        => Clone(Duration, Easing, Extras with { AnchorX = 0, AnchorY = 1 }, PageMotion);

    public Transition AnchorBottomRight()
        => Clone(Duration, Easing, Extras with { AnchorX = 1, AnchorY = 1 }, PageMotion);

    /// <summary>
    /// Include rotation in the FLIP invert, in degrees, then play back to rest.
    /// Pop uses the negated angle.
    /// </summary>
    public Transition Rotate(double degrees)
        => Clone(Duration, Easing, Extras with { Rotation = degrees }, PageMotion);

    /// <summary>
    /// Extra translation added to the FLIP invert, in device-independent pixels.
    /// Pop uses the negated offset.
    /// </summary>
    public Transition Translate(double x, double y)
        => Clone(Duration, Easing, Extras with { TranslationX = x, TranslationY = y }, PageMotion);

    /// <summary>
    /// Incoming page fades in. With a hero, only non-hero chrome fades.
    /// </summary>
    public Transition Fade()
        => Clone(Duration, Easing, Extras, PageMotion with { Fade = true });

    /// <summary>
    /// Incoming page slides in from <paramref name="edge"/>. Pop uses the opposite edge.
    /// </summary>
    public Transition SlideFrom(SlideEdge edge)
        => Clone(Duration, Easing, Extras, PageMotion with { Slide = edge });

    /// <summary>
    /// Incoming page scales from <paramref name="from"/> to 1. Default <c>0.92</c>.
    /// </summary>
    public Transition Scale(double from = 0.92)
        => Clone(Duration, Easing, Extras, PageMotion with { ScaleFrom = from });

    /// <summary>
    /// Incoming page grows from the tagged source view's frame. Pop shrinks
    /// dest back to that frame, then pops. Source must use <c>.Hero(tag)</c>
    /// so the frame can be measured. Page-only (ignored while heroes fly).
    /// </summary>
    public Transition Expand(string tag)
        => Clone(Duration, Easing, Extras, PageMotion with { ExpandTag = tag });

    internal Transition WithExtras(FlipExtras extras)
        => Clone(Duration, Easing, extras, PageMotion);

    internal Transition WithPage(PageRecipe page)
        => Clone(Duration, Easing, Extras, page);

    public abstract Transition Merge(Transition other);

    public static Transition operator |(Transition left, Transition right) => left.Merge(right);

    private protected abstract Transition Clone(
        uint duration,
        Easing easing,
        FlipExtras extras,
        PageRecipe page);

    private protected static PageRecipe MergePage(PageRecipe left, PageRecipe right)
        => PageRecipe.Merge(left, right);

    protected static uint MergeDuration(uint left, uint right)
        => right != Tween.DefaultDuration ? right : left;

    protected static Easing MergeEasing(Easing left, Easing right)
        => !ReferenceEquals(right, Tween.DefaultEasing) ? right : left;
}

internal sealed class NoneTransition : Transition
{
    public NoneTransition()
    {
    }

    public NoneTransition(uint duration, Easing easing, FlipExtras extras = default, PageRecipe page = default)
        : base(duration, easing, extras, page)
    {
    }

    public override Transition Merge(Transition other)
    {
        ArgumentNullException.ThrowIfNull(other);

        var duration = MergeDuration(Duration, other.Duration);
        var easing = MergeEasing(Easing, other.Easing);
        var extras = FlipExtras.Merge(Extras, other.Extras);
        var page = MergePage(PageMotion, other.PageMotion);
        var merged = other;
        if (duration != other.Duration)
            merged = merged.WithDuration(duration);
        if (!ReferenceEquals(easing, other.Easing))
            merged = merged.WithEasing(easing);
        if (extras != merged.Extras)
            merged = merged.WithExtras(extras);
        if (page != merged.PageMotion)
            merged = merged.WithPage(page);
        return merged;
    }

    private protected override Transition Clone(uint duration, Easing easing, FlipExtras extras, PageRecipe page)
        => new NoneTransition(duration, easing, extras, page);
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
