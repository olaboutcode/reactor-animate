using Reactor.Animate.Animation;
using Reactor.Animate.Motion;
using Xunit;

namespace MauiAnimate.Tests;

public sealed class MotionPinTests
{
    [Fact]
    public void Pinned_target_skips_all_writes_until_unpin()
    {
        var box = new BoxView { Opacity = 1 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.FadeIn().WithDuration(300).WithEasing(Easing.Linear),
            [box],
            clock);

        FlightPins.Pin([box]);
        _ = player.ForwardAsync();
        Assert.Equal(1, box.Opacity);
        clock.Tick(150);
        Assert.Equal(1, box.Opacity);

        FlightPins.Unpin();
        clock.Tick(1);
        Assert.True(box.Opacity < 1);

        FlightPins.Unpin();
        player.Dispose();
    }
}
