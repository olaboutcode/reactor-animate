namespace Reactor.Animate;

/// <summary>
/// Whole-page enter recipes. Combine with flags (fade + slide, and so on).
/// </summary>
[Flags]
public enum PageEnter
{
    None = 0,
    Fade = 1,
    SlideFromRight = 2,
    SlideFromBottom = 4,
    Scale = 8,
}

/// <summary>
/// Composable navigation recipes. Combine with <c>|</c>.
/// </summary>
public sealed class Transition
{
    public static Transition None { get; } = new();

    public IReadOnlyList<string> HeroTags { get; private init; } = [];

    public string? ExpandFromTag { get; private init; }

    public PageEnter PageEnter { get; private init; }

    public uint Duration { get; private init; } = Motion.DefaultDuration;

    public Easing Easing { get; private init; } = Motion.DefaultEasing;

    public static Transition Hero(params string[] tags) => new() { HeroTags = [.. tags] };

    public static Transition ExpandFrom(string tag) => new() { ExpandFromTag = tag };

    public static Transition Page(PageEnter enter) => new() { PageEnter = enter };

    public Transition WithDuration(uint milliseconds) => Merge(this, new Transition { Duration = milliseconds });

    public Transition WithEasing(Easing easing) => Merge(this, new Transition { Easing = easing });

    public static Transition operator |(Transition left, Transition right) => Merge(left, right);

    static Transition Merge(Transition left, Transition right)
    {
        var tags = left.HeroTags.Concat(right.HeroTags).Distinct(StringComparer.Ordinal).ToArray();
        return new Transition
        {
            HeroTags = tags,
            ExpandFromTag = right.ExpandFromTag ?? left.ExpandFromTag,
            PageEnter = left.PageEnter | right.PageEnter,
            Duration = right.Duration != Motion.DefaultDuration ? right.Duration : left.Duration,
            Easing = !ReferenceEquals(right.Easing, Motion.DefaultEasing) ? right.Easing : left.Easing,
        };
    }
}
