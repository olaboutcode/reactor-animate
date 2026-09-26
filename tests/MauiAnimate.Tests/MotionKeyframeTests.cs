using Reactor.Animate;
using Reactor.Animate.Animation;
using Reactor.Animate.Motion;
using Xunit;

namespace MauiAnimate.Tests;

public sealed class MotionKeyframeTests
{
    [Fact]
    public void Opacity_keyframes_lerp_segments()
    {
        var box = new BoxView { Opacity = 1 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None
                .Opacity(k => k.At(0, 0).At(0.5, 1).At(1, 0.2))
                .WithDuration(300)
                .WithEasing(Easing.Linear),
            [box],
            clock);

        _ = player.ForwardAsync();
        Assert.Equal(0, box.Opacity);
        clock.Tick(150);
        Assert.Equal(1, box.Opacity, 3);
        clock.Tick(150);
        Assert.Equal(0.2, box.Opacity, 3);
    }

    [Fact]
    public void First_offset_after_zero_holds_from()
    {
        var box = new BoxView { Opacity = 0.3 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None
                .Opacity(k => k.At(0.5, 1))
                .WithDuration(300)
                .WithEasing(Easing.Linear),
            [box],
            clock);

        _ = player.ForwardAsync();
        Assert.Equal(0.3, box.Opacity, 3);
        clock.Tick(149);
        Assert.Equal(0.3, box.Opacity, 3);
        clock.Tick(1);
        Assert.Equal(1, box.Opacity, 3);
    }

    [Fact]
    public void Keyframes_sugar_holds_omitted_property()
    {
        var box = new BoxView { Opacity = 1, ScaleX = 1, ScaleY = 1 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.Keyframes(
                    (0.00, s => s.Opacity(0).Scale(0.8)),
                    (0.50, s => s.Opacity(1).Scale(1.1)),
                    (1.00, s => s.Scale(1)))
                .WithDuration(200)
                .WithEasing(Easing.Linear),
            [box],
            clock);

        _ = player.ForwardAsync();
        Assert.Equal(0, box.Opacity);
        Assert.Equal(0.8, box.ScaleX, 3);
        clock.Tick(100);
        Assert.Equal(1, box.Opacity, 3);
        Assert.Equal(1.1, box.ScaleX, 3);
        clock.Tick(100);
        Assert.Equal(1, box.Opacity, 3);
        Assert.Equal(1, box.ScaleX, 3);
        Assert.Equal(1, box.ScaleY, 3);
    }

    [Fact]
    public void Empty_keyframe_builder_is_noop()
    {
        var motion = Motion.None.Opacity(k => k);
        Assert.Empty(motion.Tracks);
    }
}
