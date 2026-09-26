using Reactor.Animate.Animation;
using Reactor.Animate.Motion;
using Xunit;

namespace MauiAnimate.Tests;

public sealed class MotionSeekTests
{
    [Fact]
    public void Linear_SeekFraction_0_4_matches_opacity()
    {
        var box = new BoxView { Opacity = 1 };
        var player = MotionPlayer.Create(
            Motion.None.Opacity(0, 1).WithDuration(300).WithEasing(Easing.Linear),
            [box],
            new MotionClock(manual: true));

        player.SeekFraction(0.4);
        Assert.Equal(0.4, box.Opacity, 3);
        Assert.Equal(0.4, player.Progress, 3);
    }

    [Fact]
    public void CubicOut_SeekFraction_0_5_matches_eased_pixel_not_half_duration()
    {
        var box = new BoxView { Opacity = 1 };
        var player = MotionPlayer.Create(
            Motion.None.Opacity(0, 1).WithDuration(300).WithEasing(Easing.CubicOut),
            [box],
            new MotionClock(manual: true));

        player.SeekFraction(0.5);
        Assert.Equal(0.5, box.Opacity, 3);
        Assert.Equal(0.5, player.Progress, 3);

        var halfDuration = Easing.CubicOut.Ease(0.5);
        Assert.NotEqual(halfDuration, box.Opacity, 2);
    }

    [Fact]
    public void Seek_milliseconds_is_linear_wall_clock()
    {
        var box = new BoxView { Opacity = 1 };
        var player = MotionPlayer.Create(
            Motion.None.Opacity(0, 1).WithDuration(300).WithEasing(Easing.CubicOut),
            [box],
            new MotionClock(manual: true));

        player.Seek(150);
        var expected = Easing.CubicOut.Ease(0.5);
        Assert.Equal(expected, box.Opacity, 3);
        Assert.Equal(expected, player.Progress, 3);
    }
}
