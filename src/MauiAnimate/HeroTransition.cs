namespace Reactor.Animate;

/// <summary>
/// A shared-element page flight. <see cref="Hero(string[])"/> adds tags.
/// <see cref="Duration"/> and <see cref="Easing"/> are the push clock.
/// Pop uses <see cref="PopDuration"/> and <see cref="PopEasing"/>, which fall
/// back to that push clock. <see cref="Delay"/> and <see cref="PopDelay"/>
/// wait before that clock, with the page left as it is. Chrome fade applies to the whole flight.
/// </summary>
public sealed class HeroTransition
{
    readonly uint? _popDuration;
    readonly Easing? _popEasing;
    readonly uint? _delay;
    readonly uint? _popDelay;
    readonly HeroLayer[] _layers;

    internal static HeroTransition Empty { get; } = new(
        Timing.PageDuration, Timing.PageEasing, null, null, null, null, [], true);

    HeroTransition(
        uint duration,
        Easing easing,
        uint? popDuration,
        Easing? popEasing,
        uint? delay,
        uint? popDelay,
        HeroLayer[] layers,
        bool fadeChrome)
    {
        Duration = duration;
        Easing = easing;
        _popDuration = popDuration;
        _popEasing = popEasing;
        _delay = delay;
        _popDelay = popDelay;
        _layers = layers;
        FadeChrome = fadeChrome;
        Tags = [.. layers.SelectMany(layer => layer.Tags).Distinct(StringComparer.Ordinal)];
    }

    /// <summary>
    /// Push length in milliseconds. Pop uses <see cref="PopDuration"/>. Default is 400.
    /// </summary>
    public uint Duration { get; }

    /// <summary>
    /// Push curve. Pop uses <see cref="PopEasing"/>. Default is <see cref="Easing.CubicOut"/>.
    /// </summary>
    public Easing Easing { get; }

    /// <summary>
    /// Pop length in milliseconds. The push length when <see cref="WithPopDuration"/> was not called.
    /// </summary>
    public uint PopDuration => _popDuration ?? Duration;

    /// <summary>
    /// Pop curve. The push curve when <see cref="WithPopEasing"/> was not called.
    /// </summary>
    public Easing PopEasing => _popEasing ?? Easing;

    /// <summary>
    /// Push hold in milliseconds, before the morph. 0 when <see cref="WithDelay"/> was not called.
    /// </summary>
    public uint Delay => _delay ?? 0;

    /// <summary>
    /// Pop hold in milliseconds, before the return morph.
    /// The push hold when <see cref="WithPopDelay"/> was not called.
    /// </summary>
    public uint PopDelay => _popDelay ?? Delay;

    /// <summary>Shared-element tags on this flight. Empty when there are none.</summary>
    public IReadOnlyList<string> Tags { get; }

    internal bool FadeChrome { get; }

    /// <summary>Sets the push length in milliseconds. Does not clear <see cref="PopDuration"/>.</summary>
    public HeroTransition WithDuration(uint milliseconds)
        => new(milliseconds, Easing, _popDuration, _popEasing, _delay, _popDelay, _layers, FadeChrome);

    /// <summary>
    /// Sets the pop length in milliseconds. Push keeps <see cref="Duration"/>.
    /// A later <see cref="WithDuration"/> does not clear this.
    /// </summary>
    public HeroTransition WithPopDuration(uint milliseconds)
        => new(Duration, Easing, milliseconds, _popEasing, _delay, _popDelay, _layers, FadeChrome);

    /// <summary>Replaces the push curve. Does not clear <see cref="PopEasing"/>.</summary>
    public HeroTransition WithEasing(Easing easing)
    {
        ArgumentNullException.ThrowIfNull(easing);
        return new(Duration, easing, _popDuration, _popEasing, _delay, _popDelay, _layers, FadeChrome);
    }

    /// <summary>
    /// Sets the pop curve. Push keeps <see cref="Easing"/>.
    /// A later <see cref="WithEasing"/> does not clear this.
    /// </summary>
    public HeroTransition WithPopEasing(Easing easing)
    {
        ArgumentNullException.ThrowIfNull(easing);
        return new(Duration, Easing, _popDuration, easing, _delay, _popDelay, _layers, FadeChrome);
    }

    /// <summary>
    /// Waits <paramref name="milliseconds"/> before the push flight starts.
    /// The page on screen stays as it is. Does not change <see cref="Duration"/>
    /// and does not clear <see cref="PopDelay"/>. <c>0</c> is an explicit hold of none.
    /// </summary>
    public HeroTransition WithDelay(uint milliseconds)
        => new(Duration, Easing, _popDuration, _popEasing, milliseconds, _popDelay, _layers, FadeChrome);

    /// <summary>
    /// Waits <paramref name="milliseconds"/> before the pop flight starts.
    /// The page on screen stays as it is. Push keeps <see cref="Delay"/>.
    /// A later <see cref="WithDelay"/> does not clear this.
    /// <c>0</c> is an explicit hold of none, including when the push hold is longer.
    /// </summary>
    public HeroTransition WithPopDelay(uint milliseconds)
        => new(Duration, Easing, _popDuration, _popEasing, _delay, milliseconds, _layers, FadeChrome);

    /// <summary>
    /// Do not fade non-hero chrome on this flight. Dest Motion chrome can rest
    /// at TranslationX 0. Default still fades. TranslationX/Y skip remains.
    /// </summary>
    public HeroTransition WithoutChromeFade()
        => new(Duration, Easing, _popDuration, _popEasing, _delay, _popDelay, _layers, fadeChrome: false);

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
        return Add([tag], Configure(configure));
    }

    /// <summary>
    /// Flies every tag in <paramref name="tags"/> with the same
    /// <paramref name="configure"/> result.
    /// </summary>
    public HeroTransition Hero(IReadOnlyList<string> tags, Func<Hero, Hero> configure)
    {
        ArgumentNullException.ThrowIfNull(tags);
        ArgumentNullException.ThrowIfNull(configure);
        return Add(tags, Configure(configure));
    }

    /// <summary>
    /// Combines this flight with <paramref name="other"/>. A later duration or easing
    /// replaces the default. An explicit pop duration, pop easing, push delay, or
    /// pop delay on <paramref name="other"/> replaces one on this flight. A missing
    /// override leaves this flight's override in place. Hero layers append.
    /// </summary>
    public HeroTransition Merge(HeroTransition other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var duration = other.Duration != Timing.PageDuration ? other.Duration : Duration;
        var easing = !ReferenceEquals(other.Easing, Timing.PageEasing) ? other.Easing : Easing;
        var popDuration = other._popDuration ?? _popDuration;
        var popEasing = other._popEasing ?? _popEasing;
        var delay = other._delay ?? _delay;
        var popDelay = other._popDelay ?? _popDelay;
        return new HeroTransition(
            duration,
            easing,
            popDuration,
            popEasing,
            delay,
            popDelay,
            [.. _layers, .. other._layers],
            FadeChrome && other.FadeChrome);
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
        => new(Duration, Easing, _popDuration, _popEasing, _delay, _popDelay, [.. _layers, new HeroLayer(tags, hero.Extras)], FadeChrome);

    private static Hero Configure(Func<Hero, Hero> configure)
    {
        var hero = configure(new Hero(default))
            ?? throw new ArgumentNullException(nameof(configure), "The callback returned null.");
        return hero;
    }

    private readonly record struct HeroLayer(IReadOnlyList<string> Tags, FlipExtras Extras);
}
