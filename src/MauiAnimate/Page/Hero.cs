namespace Reactor.Animate.Page;

internal readonly record struct HeroLayer(IReadOnlyList<string> Tags, FlipExtras Extras);

/// <summary>
/// Shared-element transition. Matching views are tagged in the tree with
/// <see cref="Reactor.Animate.HeroExtensions.Hero"/>. Per-item: <c>Hero("a", h => h.AnchorCenter())</c>.
/// Shared: <c>Hero("c", "d").AnchorCenter()</c>.
/// </summary>
internal class Hero : Transition
{
    internal Hero(params string[] tags)
        : this([new HeroLayer(tags, default)], Timing.PageDuration, Timing.PageEasing)
    {
    }

    internal Hero(IReadOnlyList<HeroLayer> layers, uint duration, Easing easing)
        : base(duration, easing, layers.Count > 0 ? layers[^1].Extras : default)
    {
        Layers = layers;
        Tags = [.. layers.SelectMany(layer => layer.Tags).Distinct(StringComparer.Ordinal)];
    }

    internal IReadOnlyList<HeroLayer> Layers { get; }

    public override IReadOnlyList<string> Tags { get; }

    internal override FlipExtras ExtrasFor(string tag)
    {
        for (var i = Layers.Count - 1; i >= 0; i--)
        {
            if (Layers[i].Tags.Contains(tag, StringComparer.Ordinal))
                return Layers[i].Extras;
        }

        return Extras;
    }

    public override Transition Merge(Transition other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (other is NoneTransition)
            return Clone(
                MergeDuration(Duration, other.Duration),
                MergeEasing(Easing, other.Easing),
                FlipExtras.Merge(Extras, other.Extras));

        if (other is Hero hero)
        {
            return new Hero(
                [.. Layers, .. hero.Layers],
                MergeDuration(Duration, hero.Duration),
                MergeEasing(Easing, hero.Easing));
        }

        return new Hero(
            Layers,
            MergeDuration(Duration, other.Duration),
            MergeEasing(Easing, other.Easing));
    }

    private protected override Transition Clone(uint duration, Easing easing, FlipExtras extras)
    {
        if (Layers.Count == 0)
            return new Hero([new HeroLayer([], extras)], duration, easing);

        var layers = Layers.ToArray();
        layers[^1] = layers[^1] with { Extras = extras };
        return new Hero(layers, duration, easing);
    }
}
