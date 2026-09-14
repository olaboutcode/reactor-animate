namespace Reactor.Animate;

/// <summary>
/// Shared-element transition. Matching views are tagged in the tree with
/// <see cref="HeroExtensions.Hero"/>. Merging two heroes unions their tags.
/// </summary>
public class Hero : Transition
{
    internal Hero(params string[] tags)
        : this(tags, Motion.DefaultDuration, Motion.DefaultEasing, default)
    {
    }

    internal Hero(IReadOnlyList<string> tags, uint duration, Easing easing, MotionExtras extras = default)
        : base(duration, easing, extras)
    {
        Tags = tags;
    }

    public override IReadOnlyList<string> Tags { get; }

    public override Transition Merge(Transition other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (other is NoneTransition)
            return Clone(
                MergeDuration(Duration, other.Duration),
                MergeEasing(Easing, other.Easing),
                MotionExtras.Merge(Extras, other.Extras));

        if (other is Hero hero)
        {
            var tags = Tags.Concat(hero.Tags).Distinct(StringComparer.Ordinal).ToArray();
            return new Hero(
                tags,
                MergeDuration(Duration, hero.Duration),
                MergeEasing(Easing, hero.Easing),
                MotionExtras.Merge(Extras, hero.Extras));
        }

        return new Hero(
            Tags,
            MergeDuration(Duration, other.Duration),
            MergeEasing(Easing, other.Easing),
            MotionExtras.Merge(Extras, other.Extras));
    }

    private protected override Transition Clone(uint duration, Easing easing, MotionExtras extras)
        => new Hero(Tags, duration, easing, extras);
}
