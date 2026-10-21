using Reactor.Animate;
using Xunit;

namespace MauiAnimate.Tests;

public sealed class HeroTransitionTests
{
    [Fact]
    public void Hero_keeps_each_tags_extras()
    {
        var flight = HeroTransition.Empty
            .Hero("cover", h => h.AnchorCenter())
            .Hero("spin", h => h.AnchorCenter().Rotate(90))
            .Hero(["c", "d"], h => h.Translate(8, 0))
            .WithDuration(300);

        Assert.Equal(["cover", "spin", "c", "d"], flight.Tags);
        Assert.Equal(300u, flight.Duration);
        Assert.Equal(0.5, flight.ExtrasFor("cover").AnchorX);
        Assert.Equal(0, flight.ExtrasFor("cover").Rotation);
        Assert.Equal(90, flight.ExtrasFor("spin").Rotation);
        Assert.Equal(8, flight.ExtrasFor("d").TranslationX);
    }

    [Fact]
    public void Merge_keeps_an_explicit_duration_and_appends_tags()
    {
        var left = HeroTransition.Empty.WithDuration(200).Hero("a");
        var right = HeroTransition.Empty.Hero("b").WithDuration(500);
        var merged = left.Merge(right);

        Assert.Equal(500u, merged.Duration);
        Assert.Equal(["a", "b"], merged.Tags);
    }

    [Fact]
    public void Merge_keeps_the_earlier_duration_when_the_later_one_is_the_default()
    {
        var merged = HeroTransition.Empty.WithDuration(200)
            .Merge(HeroTransition.Empty.Hero("b"));

        Assert.Equal(200u, merged.Duration);
        Assert.Equal(200u, merged.PopDuration);
        Assert.False(HeroTransition.Empty.WithoutChromeFade().FadeChrome);
    }

    [Fact]
    public void Pop_clock_falls_back_to_the_push_clock()
    {
        var flight = HeroTransition.Empty.WithDuration(300).WithEasing(Easing.SinOut);

        Assert.Equal(400u, HeroTransition.Empty.PopDuration);
        Assert.Same(Easing.CubicOut, HeroTransition.Empty.PopEasing);
        Assert.Equal(300u, flight.PopDuration);
        Assert.Same(Easing.SinOut, flight.PopEasing);
    }

    [Fact]
    public void Pop_duration_and_easing_stay_set_when_the_push_clock_changes()
    {
        var longerFirst = HeroTransition.Empty
            .WithPopDuration(200)
            .WithDuration(400)
            .WithPopEasing(Easing.CubicIn)
            .WithEasing(Easing.SinOut);
        var pushFirst = HeroTransition.Empty
            .WithDuration(400)
            .WithPopDuration(400)
            .WithDuration(500)
            .WithEasing(Easing.SinOut)
            .WithPopEasing(Easing.SinOut)
            .WithEasing(Easing.CubicIn);

        Assert.Equal(400u, longerFirst.Duration);
        Assert.Equal(200u, longerFirst.PopDuration);
        Assert.Same(Easing.SinOut, longerFirst.Easing);
        Assert.Same(Easing.CubicIn, longerFirst.PopEasing);
        Assert.Equal(500u, pushFirst.Duration);
        Assert.Equal(400u, pushFirst.PopDuration);
        Assert.Same(Easing.CubicIn, pushFirst.Easing);
        Assert.Same(Easing.SinOut, pushFirst.PopEasing);
    }

    [Fact]
    public void Pop_override_keeps_tags_and_extras()
    {
        var flight = HeroTransition.Empty
            .Hero("spin", h => h.AnchorCenter().Rotate(90))
            .WithPopDuration(200);

        Assert.Equal(["spin"], flight.Tags);
        Assert.Equal(90, flight.ExtrasFor("spin").Rotation);
        Assert.Equal(0.5, flight.ExtrasFor("spin").AnchorX);
    }

    [Fact]
    public void Merge_keeps_a_pop_override_when_the_other_flight_does_not_set_one()
    {
        var timed = HeroTransition.Empty
            .WithDuration(400)
            .WithPopDuration(200)
            .WithPopEasing(Easing.CubicIn)
            .Hero("cover");

        var withTag = timed.Merge(HeroTransition.Empty.Hero("badge"));
        var withPushDuration = timed.Merge(HeroTransition.Empty.WithDuration(500));

        Assert.Equal(["cover", "badge"], withTag.Tags);
        Assert.Equal(400u, withTag.Duration);
        Assert.Equal(200u, withTag.PopDuration);
        Assert.Same(Easing.CubicIn, withTag.PopEasing);
        Assert.Equal(500u, withPushDuration.Duration);
        Assert.Equal(200u, withPushDuration.PopDuration);
        Assert.Same(Easing.CubicIn, withPushDuration.PopEasing);
    }

    [Fact]
    public void Merge_lets_an_explicit_pop_override_replace_the_earlier_one()
    {
        var merged = HeroTransition.Empty
            .WithDuration(400)
            .WithPopDuration(200)
            .WithPopEasing(Easing.CubicIn)
            .Merge(HeroTransition.Empty.WithPopDuration(120).WithPopEasing(Easing.SinIn));

        Assert.Equal(400u, merged.Duration);
        Assert.Equal(120u, merged.PopDuration);
        Assert.Same(Easing.SinIn, merged.PopEasing);
    }

    [Fact]
    public void WithPopEasing_rejects_null()
    {
        Assert.Throws<ArgumentNullException>(() => HeroTransition.Empty.WithPopEasing(null!));
    }
}
