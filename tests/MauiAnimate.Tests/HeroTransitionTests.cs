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
        Assert.False(HeroTransition.Empty.WithoutChromeFade().FadeChrome);
    }
}
