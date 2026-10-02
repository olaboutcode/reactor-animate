using Reactor.Animate;
using Reactor.Animate.Internals;
using Xunit;

namespace MauiAnimate.Tests;

public sealed class MotionStaggerTests
{
    [Fact]
    public void Two_targets_hold_from_during_stagger_delay()
    {
        var first = new BoxView { Opacity = 1 };
        var second = new BoxView { Opacity = 1 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.FadeIn().WithDuration(300).WithEasing(Easing.Linear).Stagger(100),
            [first, second],
            clock);

        Assert.Equal(400u, player.Duration);

        _ = player.ForwardAsync();
        Assert.Equal(0, first.Opacity);
        Assert.Equal(0, second.Opacity);

        clock.Tick(99);
        Assert.True(first.Opacity > 0);
        Assert.Equal(0, second.Opacity);

        clock.Tick(1);
        Assert.Equal(0, second.Opacity);

        clock.Tick(300);
        Assert.Equal(1, first.Opacity);
        Assert.Equal(1, second.Opacity);
        Assert.Equal(MotionPlaybackStatus.Completed, player.Status);
    }

    [Fact]
    public void Stagger_from_end_delays_the_first_target()
    {
        var first = new BoxView { Opacity = 1 };
        var second = new BoxView { Opacity = 1 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.FadeIn().WithDuration(300).WithEasing(Easing.Linear).Stagger(100, StaggerFrom.End),
            [first, second],
            clock);

        _ = player.ForwardAsync();
        clock.Tick(99);
        Assert.Equal(0, first.Opacity);
        Assert.True(second.Opacity > 0);
    }
}
