using Reactor.Animate.Animation;
using Reactor.Animate.Motion;
using Xunit;

namespace MauiAnimate.Tests;

public sealed class MotionExclusiveTests
{
    [Fact]
    public void Second_bind_disposes_the_first_player_on_the_same_view()
    {
        var box = new BoxView { Opacity = 1 };
        var first = MotionPlayer.Create(
            Motion.None.FadeIn().WithDuration(300).WithEasing(Easing.Linear),
            [box],
            new MotionClock(manual: true));
        var secondClock = new MotionClock(manual: true);
        var second = MotionPlayer.Create(
            Motion.None.Opacity(0, 0.25).WithDuration(300).WithEasing(Easing.Linear),
            [box],
            secondClock);

        Assert.Throws<ObjectDisposedException>(() => first.Pause());

        var task = second.ForwardAsync();
        secondClock.Tick(300);
        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(0.25, box.Opacity, 3);
        second.Dispose();
    }

    [Fact]
    public void Claiming_one_view_disposes_a_stagger_player_that_owned_it()
    {
        var a = new BoxView { Opacity = 1 };
        var b = new BoxView { Opacity = 1 };
        var group = MotionPlayer.Create(
            Motion.None.FadeIn().WithDuration(300).WithEasing(Easing.Linear),
            [a, b],
            new MotionClock(manual: true));
        var other = MotionPlayer.Create(
            Motion.None.Opacity(0, 1).WithDuration(100).WithEasing(Easing.Linear),
            [a],
            new MotionClock(manual: true));

        Assert.Throws<ObjectDisposedException>(() => group.Pause());
        other.Dispose();
    }
}
