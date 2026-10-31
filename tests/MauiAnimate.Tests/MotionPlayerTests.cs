using Reactor.Animate;
using Reactor.Animate.Internals;
using Xunit;

namespace MauiAnimate.Tests;

public sealed class MotionPlayerTests
{
    [Fact]
    public void Progress_listener_runs_on_every_tick()
    {
        var (player, clock, _) = Linear();
        var count = 0;
        player.At(_ => count++);
        _ = player.ForwardAsync();

        var afterStart = count;
        clock.Tick(40);
        clock.Tick(40);

        Assert.True(afterStart >= 1);
        Assert.Equal(afterStart + 2, count);
    }

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

        Assert.True(forward.IsCompletedSuccessfully);
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

    [Fact]
    public void Delay_holds_from_then_plays_without_growing_duration()
    {
        var box = new BoxView { Opacity = 1 };
        var clock = new MotionClock(manual: true);
        var motion = Motion.None.Opacity(0, 1).WithDuration(300).WithEasing(Easing.Linear).WithDelay(350);

        Assert.Equal(0u, Motion.None.Delay);
        Assert.Equal(300u, motion.Duration);
        Assert.Equal(350u, motion.Delay);
        Assert.Equal(0u, motion.WithDelay(0).Delay);
        Assert.Equal(350u, motion.WithDuration(500).Delay);
        Assert.Equal(500u, motion.WithDuration(500).Duration);

        var player = MotionPlayer.Create(motion, [box], clock);
        Assert.Equal(300u, player.Duration);

        var task = player.ForwardAsync();
        Assert.Equal(0, box.Opacity);
        clock.Tick(350);
        Assert.Equal(0, box.Opacity);
        Assert.Equal(0, player.Progress);
        Assert.False(task.IsCompleted);

        clock.Tick(300);
        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(1, box.Opacity);
        Assert.Equal(1, player.Progress);
    }

    [Fact]
    public void Delay_holds_then_stagger_runs_inside_the_track_span()
    {
        var first = new BoxView { Opacity = 1 };
        var second = new BoxView { Opacity = 1 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.FadeIn().WithDuration(300).WithEasing(Easing.Linear).WithDelay(350).Stagger(100),
            [first, second],
            clock);

        Assert.Equal(400u, player.Duration);
        _ = player.ForwardAsync();
        clock.Tick(350);
        Assert.Equal(0, first.Opacity);
        Assert.Equal(0, second.Opacity);

        clock.Tick(1);
        Assert.True(first.Opacity > 0);
        Assert.Equal(0, second.Opacity);

        clock.Tick(99);
        Assert.Equal(0, second.Opacity);

        clock.Tick(1);
        Assert.True(second.Opacity > 0);
    }

    [Fact]
    public void Delay_holds_a_spring_at_from_then_lets_it_settle()
    {
        var box = new BoxView { Opacity = 1 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.Opacity(0, 1).WithSpring(Spring.Default).WithDelay(200),
            [box],
            clock);

        var task = player.ForwardAsync();
        clock.Tick(200);
        Assert.Equal(0, box.Opacity);
        Assert.Equal(0, player.Progress, 3);
        Assert.False(task.IsCompleted);

        for (var i = 0; i < 500 && player.Status != MotionPlaybackStatus.Completed; i++)
            clock.Tick(16);

        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(1, box.Opacity, 2);
    }

    [Fact]
    public void Delay_plays_once_in_front_of_repeat()
    {
        var box = new BoxView { Opacity = 1 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.Opacity(0, 1).WithDuration(100).WithEasing(Easing.Linear).WithDelay(50).Repeat(2),
            [box],
            clock);

        var task = player.ForwardAsync();
        clock.Tick(50);
        Assert.Equal(0, box.Opacity);
        Assert.False(task.IsCompleted);

        clock.Tick(100);
        Assert.False(task.IsCompleted);
        Assert.Equal(0, box.Opacity);
        Assert.Equal(MotionPlaybackStatus.Forward, player.Status);

        clock.Tick(100);
        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(1, box.Opacity);
    }

    [Fact]
    public void Delay_does_not_pause_a_yoyo_reverse()
    {
        var box = new BoxView { Opacity = 1 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.Opacity(0, 1).WithDuration(100).WithEasing(Easing.Linear).WithDelay(50).Yoyo().Repeat(1),
            [box],
            clock);

        var task = player.ForwardAsync();
        clock.Tick(50);
        Assert.Equal(0, box.Opacity);
        clock.Tick(100);
        Assert.Equal(1, box.Opacity);
        Assert.Equal(MotionPlaybackStatus.Reverse, player.Status);
        Assert.False(task.IsCompleted);

        clock.Tick(100);
        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(0, box.Opacity);
        Assert.Equal(MotionPlaybackStatus.Dismissed, player.Status);
    }

    [Fact]
    public void Reset_arms_the_delay_again()
    {
        var box = new BoxView { Opacity = 1 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.Opacity(0, 1).WithDuration(300).WithEasing(Easing.Linear).WithDelay(350),
            [box],
            clock);

        _ = player.ForwardAsync();
        clock.Tick(100);
        player.Reset();
        Assert.Equal(0, box.Opacity);
        Assert.Equal(MotionPlaybackStatus.Dismissed, player.Status);

        var task = player.ForwardAsync();
        clock.Tick(350);
        Assert.Equal(0, box.Opacity);
        Assert.False(task.IsCompleted);
        clock.Tick(300);
        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(1, box.Opacity);
    }

    [Fact]
    public void Seek_during_the_delay_drops_the_rest_of_the_hold()
    {
        var box = new BoxView { Opacity = 1 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.Opacity(0, 1).WithDuration(300).WithEasing(Easing.Linear).WithDelay(350),
            [box],
            clock);

        var task = player.ForwardAsync();
        clock.Tick(100);
        player.Seek(150);

        Assert.Equal(0.5, box.Opacity, 3);
        Assert.Equal(0.5, player.Progress, 3);
        Assert.Equal(MotionPlaybackStatus.Paused, player.Status);

        var continued = player.ForwardAsync();
        Assert.Same(task, continued);
        clock.Tick(150);
        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(1, box.Opacity);
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
