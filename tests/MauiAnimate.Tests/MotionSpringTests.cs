using Reactor.Animate;
using Reactor.Animate.Animation;
using Reactor.Animate.Motion;
using Xunit;

namespace MauiAnimate.Tests;

public sealed class MotionSpringTests
{
    [Fact]
    public void Spring_fade_settles_at_to()
    {
        var box = new BoxView { Opacity = 1 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.Opacity(0, 1).WithSpring(Spring.Default),
            [box],
            clock);

        var task = player.ForwardAsync();
        Assert.Equal(0, box.Opacity);
        TickUntil(clock, player, MotionPlaybackStatus.Completed);

        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(1, box.Opacity, 2);
        Assert.Equal(1, player.Progress, 2);
    }

    [Fact]
    public void Spring_reverse_settles_at_from()
    {
        var box = new BoxView { Opacity = 1 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.Opacity(0, 1).WithSpring(Spring.Snappy),
            [box],
            clock);

        _ = player.ForwardAsync();
        TickUntil(clock, player, MotionPlaybackStatus.Completed);
        var reverse = player.ReverseAsync();
        TickUntil(clock, player, MotionPlaybackStatus.Dismissed);

        Assert.True(reverse.IsCompletedSuccessfully);
        Assert.Equal(0, box.Opacity, 2);
    }

    static void TickUntil(MotionClock clock, MotionPlayer player, MotionPlaybackStatus expected)
    {
        for (var i = 0; i < 500; i++)
        {
            clock.Tick(16);
            if (player.Status == expected)
                return;
        }

        Assert.Fail($"spring did not reach {expected}");
    }
}
