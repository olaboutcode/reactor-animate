namespace Reactor.Animate;

/// <summary>
/// A shared-element page flight. <see cref="Hero(string[])"/> adds tags.
/// Duration, easing, and chrome fade apply to the whole flight.
/// </summary>
public sealed class HeroTransition
{
    readonly Layer[] _layers;

    internal static HeroTransition Empty { get; } = new(Timing.PageDuration, Timing.PageEasing, [], true);

    HeroTransition(uint duration, Easing easing, Layer[] layers, bool fadeChrome)
    {
        Duration = duration;
        Easing = easing;
        _layers = layers;
        FadeChrome = fadeChrome;
        Tags = [.. layers.SelectMany(layer => layer.Tags).Distinct(StringComparer.Ordinal)];
    }

    /// <summary>Flight length in milliseconds. Default is 400.</summary>
    public uint Duration { get; }

    /// <summary>Flight curve. Default is <see cref="Easing.CubicOut"/>.</summary>
    public Easing Easing { get; }

    /// <summary>Shared-element tags on this flight. Empty when there are none.</summary>
    public IReadOnlyList<string> Tags { get; }

    internal bool FadeChrome { get; }

    /// <summary>Sets the flight length in milliseconds.</summary>
    public HeroTransition WithDuration(uint milliseconds)
        => new(milliseconds, Easing, _layers, FadeChrome);

    /// <summary>Replaces the flight curve.</summary>
    public HeroTransition WithEasing(Easing easing)
        => new(Duration, easing, _layers, FadeChrome);

    /// <summary>
    /// Do not fade non-hero chrome on this flight. Dest Motion chrome can rest
    /// at TranslationX 0. Default still fades. TranslationX/Y skip remains.
    /// </summary>
    public HeroTransition WithoutChromeFade()
        => new(Duration, Easing, _layers, fadeChrome: false);

    /// <summary>Flies the views tagged with <paramref name="tags"/>.</summary>
    public HeroTransition Hero(params string[] tags)
    {
        ArgumentNullException.ThrowIfNull(tags);
        return Add(tags, new Hero(default));
    }

    /// <summary>
    /// Flies the view tagged with <paramref name="tag"/>, using
    /// <paramref name="configure"/> for its anchor, rotation, or translation.
    /// </summary>
    public HeroTransition Hero(string tag, Func<Hero, Hero> configure)
    {
        ArgumentNullException.ThrowIfNull(tag);
        ArgumentNullException.ThrowIfNull(configure);
        return Add([tag], Configured(configure));
    }

    /// <summary>
    /// Flies every tag in <paramref name="tags"/> with the same
    /// <paramref name="configure"/> result.
    /// </summary>
    public HeroTransition Hero(IReadOnlyList<string> tags, Func<Hero, Hero> configure)
    {
        ArgumentNullException.ThrowIfNull(tags);
        ArgumentNullException.ThrowIfNull(configure);
        return Add(tags, Configured(configure));
    }

    /// <summary>
    /// Combines this flight with <paramref name="other"/>. A later duration or easing
    /// replaces the default. Hero layers append.
    /// </summary>
    public HeroTransition Merge(HeroTransition other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var duration = other.Duration != Timing.PageDuration ? other.Duration : Duration;
        var easing = !ReferenceEquals(other.Easing, Timing.PageEasing) ? other.Easing : Easing;
        return new HeroTransition(duration, easing, [.. _layers, .. other._layers], FadeChrome && other.FadeChrome);
    }

    internal FlipExtras ExtrasFor(string tag)
    {
        for (var i = _layers.Length - 1; i >= 0; i--)
        {
            if (_layers[i].Tags.Contains(tag, StringComparer.Ordinal))
                return _layers[i].Extras;
        }

        return _layers.Length > 0 ? _layers[^1].Extras : default;
    }

    HeroTransition Add(IReadOnlyList<string> tags, Hero hero)
        => new(Duration, Easing, [.. _layers, new Layer(tags, hero.Extras)], FadeChrome);

    static Hero Configured(Func<Hero, Hero> configure)
    {
        var hero = configure(new Hero(default));
        ArgumentNullException.ThrowIfNull(hero);
        return hero;
    }

    readonly record struct Layer(IReadOnlyList<string> Tags, FlipExtras Extras);
}
