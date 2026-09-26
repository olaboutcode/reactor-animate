using Reactor.Animate;
using Reactor.Animate.Animation;
using Reactor.Animate.Motion;
using Xunit;

namespace MauiAnimate.Tests;

public sealed class MotionRepeatTests
{
    [Fact]
    public void Repeat_2_plays_forward_twice_without_recapture()
    {
        var box = new BoxView { Opacity = 1 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.Opacity(0, 1).WithDuration(100).WithEasing(Easing.Linear).Repeat(2),
            [box],
            clock);

        var task = player.ForwardAsync();
        clock.Tick(100);
        Assert.False(task.IsCompleted);
        Assert.Equal(0, box.Opacity);
        Assert.Equal(MotionPlaybackStatus.Forward, player.Status);

        clock.Tick(100);
        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(1, box.Opacity);
        Assert.Equal(MotionPlaybackStatus.Completed, player.Status);
    }

    [Fact]
    public void Yoyo_repeat_1_forwards_then_reverses_to_dismissed()
    {
        var box = new BoxView { Opacity = 1 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.Opacity(0, 1).WithDuration(100).WithEasing(Easing.Linear).Yoyo().Repeat(1),
            [box],
            clock);

        var task = player.ForwardAsync();
        clock.Tick(100);
        Assert.False(task.IsCompleted);
        Assert.Equal(1, box.Opacity);
        Assert.Equal(MotionPlaybackStatus.Reverse, player.Status);

        clock.Tick(100);
        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(0, box.Opacity);
        Assert.Equal(MotionPlaybackStatus.Dismissed, player.Status);
    }

    [Fact]
    public void Infinite_repeat_stops_on_dispose()
    {
        var box = new BoxView { Opacity = 1 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.Opacity(0, 1).WithDuration(100).WithEasing(Easing.Linear).Repeat(-1),
            [box],
            clock);

        var task = player.ForwardAsync();
        clock.Tick(100);
        Assert.False(task.IsCompleted);
        player.Dispose();
        Assert.True(task.IsCanceled);
        clock.Tick(100);
    }

    [Fact]
    public void Reverse_on_yoyo_completes_forward_without_cancel_and_stops_loop()
    {
        var box = new BoxView { Opacity = 1 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.Opacity(0, 1).WithDuration(100).WithEasing(Easing.Linear).Yoyo().Repeat(-1),
            [box],
            clock);

        var forward = player.ForwardAsync();
        clock.Tick(40);
        var reverse = player.ReverseAsync();

        Assert.True(forward.IsCompletedSuccessfully);
        clock.Tick(40);
        Assert.True(reverse.IsCompletedSuccessfully);
        Assert.Equal(0, box.Opacity);
        Assert.Equal(MotionPlaybackStatus.Dismissed, player.Status);
        clock.Tick(100);
        Assert.Equal(MotionPlaybackStatus.Dismissed, player.Status);
    }
}
