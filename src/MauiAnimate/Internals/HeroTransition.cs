using Reactor.Animate;

namespace Reactor.Animate.Internals;

internal readonly record struct HeroLayer(IReadOnlyList<string> Tags, FlipExtras Extras);

/// <summary>
/// Shared-element transition. Matching views are tagged in the tree with
/// <see cref="VisualNodeExtensions.Hero(MauiReactor.VisualNode, string)"/>.
/// Per-item: <c>Hero("a", h => h.AnchorCenter())</c>.
/// Shared: <c>Hero("c", "d").AnchorCenter()</c>.
/// </summary>
internal class HeroTransition : Transition
{
    internal HeroTransition(params string[] tags)
        : this([new HeroLayer(tags, default)], Timing.PageDuration, Timing.PageEasing)
    {
    }

    internal HeroTransition(IReadOnlyList<HeroLayer> layers, uint duration, Easing easing, bool fadeChrome = true)
        : base(duration, easing, layers.Count > 0 ? layers[^1].Extras : default, fadeChrome)
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
                MergeFadeChrome(FadeChrome, other.FadeChrome));

        if (other is HeroTransition hero)
        {
            return new HeroTransition(
                [.. Layers, .. hero.Layers],
                MergeDuration(Duration, hero.Duration),
                MergeEasing(Easing, hero.Easing),
                MergeFadeChrome(FadeChrome, hero.FadeChrome));
        }

        return new HeroTransition(
            Layers,
            MergeDuration(Duration, other.Duration),
            MergeEasing(Easing, other.Easing),
            MergeFadeChrome(FadeChrome, other.FadeChrome));
    }

    private protected override Transition Clone(uint duration, Easing easing, FlipExtras extras, bool fadeChrome)
    {
        if (Layers.Count == 0)
            return new HeroTransition([new HeroLayer([], extras)], duration, easing, fadeChrome);

        var layers = Layers.ToArray();
        layers[^1] = layers[^1] with { Extras = extras };
        return new HeroTransition(layers, duration, easing, fadeChrome);
    }
}
