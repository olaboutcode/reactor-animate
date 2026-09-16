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
        : this([new HeroLayer(tags, default)], Tween.DefaultDuration, Tween.DefaultEasing)
    {
    }

    internal Hero(IReadOnlyList<HeroLayer> layers, uint duration, Easing easing, PageRecipe page = default)
        : base(duration, easing, layers.Count > 0 ? layers[^1].Extras : default, page)
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
                FlipExtras.Merge(Extras, other.Extras),
                MergePage(PageMotion, other.PageMotion));

        if (other is Hero hero)
        {
            return new Hero(
                [.. Layers, .. hero.Layers],
                MergeDuration(Duration, hero.Duration),
                MergeEasing(Easing, hero.Easing),
                MergePage(PageMotion, hero.PageMotion));
        }

        return new Hero(
            Layers,
            MergeDuration(Duration, other.Duration),
            MergeEasing(Easing, other.Easing),
            MergePage(PageMotion, other.PageMotion));
    }

    private protected override Transition Clone(uint duration, Easing easing, FlipExtras extras, PageRecipe page)
    {
        if (Layers.Count == 0)
            return new Hero([new HeroLayer([], extras)], duration, easing, page);

        var layers = Layers.ToArray();
        layers[^1] = layers[^1] with { Extras = extras };
        return new Hero(layers, duration, easing, page);
    }
}
