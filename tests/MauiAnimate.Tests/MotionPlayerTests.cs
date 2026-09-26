using Reactor.Animate;
using Reactor.Animate.Animation;
using Reactor.Animate.Motion;
using Xunit;

namespace MauiAnimate.Tests;

public sealed class MotionPlayerTests
{
    [Fact]
    public void Forward_writes_opacity_and_completes()
    {
        var (player, clock, box) = Linear();
        var task = player.ForwardAsync();

        Assert.Equal(MotionPlaybackStatus.Forward, player.Status);
        Assert.Equal(0, box.Opacity);
        clock.Tick(300);

        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(1, box.Opacity);
        Assert.Equal(MotionPlaybackStatus.Completed, player.Status);
        Assert.Equal(1, player.Progress);
    }

    [Fact]
    public void Pause_at_linear_0_4()
    {
        var (player, clock, box) = Linear();
        _ = player.ForwardAsync();
        clock.Tick(120);
        player.Pause();

        Assert.Equal(MotionPlaybackStatus.Paused, player.Status);
        Assert.Equal(0.4, box.Opacity, 3);
        clock.Tick(120);
        Assert.Equal(0.4, box.Opacity, 3);
    }

    [Fact]
    public void Reverse_while_playing_cancels_forward_and_returns_to_from()
    {
        var (player, clock, box) = Linear();
        var forward = player.ForwardAsync();
        clock.Tick(150);
        var reverse = player.ReverseAsync();

        Assert.True(forward.IsCanceled);
        Assert.Equal(MotionPlaybackStatus.Reverse, player.Status);
        clock.Tick(150);

        Assert.True(reverse.IsCompletedSuccessfully);
        Assert.Equal(0, box.Opacity);
        Assert.Equal(MotionPlaybackStatus.Dismissed, player.Status);
    }

    [Fact]
    public void Token_cancel_then_resume_allocates_a_new_task()
    {
        var (player, clock, box) = Linear();
        using var cts = new CancellationTokenSource();
        var first = player.ForwardAsync(cts.Token);
        clock.Tick(50);
        cts.Cancel();

        Assert.True(first.IsCanceled);
        Assert.Equal(MotionPlaybackStatus.Paused, player.Status);

        player.Resume();
        var second = player.ForwardAsync();
        Assert.NotSame(first, second);
        Assert.False(second.IsCompleted);
        clock.Tick(300);
        Assert.True(second.IsCompletedSuccessfully);
        Assert.Equal(1, box.Opacity);
    }

    [Fact]
    public void SeekFraction_1_completes()
    {
        var (player, clock, box) = Linear();
        var task = player.ForwardAsync();
        clock.Tick(30);
        player.SeekFraction(1);

        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(1, box.Opacity);
        Assert.Equal(MotionPlaybackStatus.Completed, player.Status);
    }

    [Fact]
    public void Dispose_cancels_and_further_calls_throw()
    {
        var (player, clock, _) = Linear();
        var task = player.ForwardAsync();
        player.Dispose();

        Assert.True(task.IsCanceled);
        Assert.Throws<ObjectDisposedException>(() => player.Pause());
        clock.Tick(300);
    }

    [Fact]
    public void Forward_from_completed_is_noop()
    {
        var (player, clock, box) = Linear();
        var first = player.ForwardAsync();
        clock.Tick(300);
        Assert.True(first.IsCompletedSuccessfully);

        var second = player.ForwardAsync();
        Assert.True(second.IsCompletedSuccessfully);
        Assert.Equal(MotionPlaybackStatus.Completed, player.Status);
        Assert.Equal(1, box.Opacity);
    }

    [Fact]
    public void Reverse_from_dismissed_is_noop()
    {
        var (player, _, box) = Linear();
        var task = player.ReverseAsync();
        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(MotionPlaybackStatus.Dismissed, player.Status);
        Assert.Equal(1, box.Opacity);
    }

    [Fact]
    public void Duration_zero_completes_synchronously()
    {
        var box = new BoxView { Opacity = 1 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.Opacity(0, 1).WithDuration(0).WithEasing(Easing.Linear),
            [box],
            clock);
        var started = 0;
        var completed = 0;
        player.Started += (_, _) => started++;
        player.Completed += (_, _) => completed++;

        var task = player.ForwardAsync();
        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(1, box.Opacity);
        Assert.Equal(MotionPlaybackStatus.Completed, player.Status);
        Assert.Equal(1, started);
        Assert.Equal(1, completed);
    }

    static (MotionPlayer Player, MotionClock Clock, BoxView Box) Linear(uint duration = 300)
    {
        var box = new BoxView { Opacity = 1 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.Opacity(0, 1).WithDuration(duration).WithEasing(Easing.Linear),
            [box],
            clock);
        return (player, clock, box);
    }
}
