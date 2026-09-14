namespace Reactor.Animate;

/// <summary>
/// Shared-element transition. Matching views are tagged in the tree with
/// <see cref="HeroExtensions.Hero"/>. Merging two heroes unions their tags.
/// </summary>
public class Hero : Transition
{
    public Hero(params string[] tags)
        : this(tags, Motion.DefaultDuration, Motion.DefaultEasing)
    {
    }

    Hero(IReadOnlyList<string> tags, uint duration, Easing easing)
        : base(duration, easing)
    {
        Tags = tags;
    }

    public override IReadOnlyList<string> Tags { get; }

    public override Transition Merge(Transition other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (other is NoneTransition)
            return this;

        if (other is Hero hero)
        {
            var tags = Tags.Concat(hero.Tags).Distinct(StringComparer.Ordinal).ToArray();
            return new Hero(tags, MergeDuration(Duration, hero.Duration), MergeEasing(Easing, hero.Easing));
        }

        return new Hero(Tags, MergeDuration(Duration, other.Duration), MergeEasing(Easing, other.Easing));
    }

    protected override Transition Clone(uint duration, Easing easing)
        => new Hero(Tags, duration, easing);
}
