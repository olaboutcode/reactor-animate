using Reactor.Animate;
using Reactor.Animate.Internals;
using Xunit;

namespace MauiAnimate.Tests;

public sealed class MotionTimelineTests
{
    [Fact]
    public void Then_appends_at_current_span()
    {
        var box = new BoxView { Opacity = 1 };
        var clock = new MotionClock(manual: true);
        var motion = Motion.None.FadeIn().WithDuration(200).WithEasing(Easing.Linear)
            .Then(Motion.None.Opacity(1, 0).WithDuration(200).WithEasing(Easing.Linear));
        var player = MotionPlayer.Create(motion, [box], clock);

        Assert.Equal(400u, motion.Duration);
        Assert.Equal(400u, player.Duration);

        _ = player.ForwardAsync();
        clock.Tick(200);
        Assert.Equal(1, box.Opacity, 3);
        clock.Tick(200);
        Assert.Equal(0, box.Opacity, 3);
        Assert.Equal(MotionPlaybackStatus.Completed, player.Status);
    }

    [Fact]
    public void Add_at_shifts_child_without_stretching()
    {
        var box = new BoxView { Opacity = 1, TranslationX = 0 };
        var clock = new MotionClock(manual: true);
        var motion = Motion.None.FadeIn().WithDuration(300).WithEasing(Easing.Linear)
            .Add(Motion.None.TranslateX(0, 90).WithDuration(300).WithEasing(Easing.Linear), 80);
        var player = MotionPlayer.Create(motion, [box], clock);

        Assert.Equal(380u, motion.Duration);

        _ = player.ForwardAsync();
        clock.Tick(80);
        Assert.Equal(0, box.TranslationX, 3);
        Assert.True(box.Opacity > 0);
        clock.Tick(220);
        Assert.Equal(1, box.Opacity, 3);
        clock.Tick(80);
        Assert.Equal(90, box.TranslationX, 3);
    }

    [Fact]
    public void Nested_group_flattens_into_parent()
    {
        var box = new BoxView { Opacity = 1, TranslationX = 0 };
        var clock = new MotionClock(manual: true);
        var group = Motion.None.FadeIn().WithDuration(100).WithEasing(Easing.Linear)
            .Then(Motion.None.FadeOut().WithDuration(100).WithEasing(Easing.Linear));
        var motion = group.Add(
            Motion.None.TranslateX(0, 40).WithDuration(200).WithEasing(Easing.Linear),
            0);
        var player = MotionPlayer.Create(motion, [box], clock);

        Assert.Equal(200u, motion.Duration);
        _ = player.ForwardAsync();
        clock.Tick(100);
        Assert.Equal(1, box.Opacity, 3);
        Assert.Equal(20, box.TranslationX, 3);
        clock.Tick(100);
        Assert.Equal(0, box.Opacity, 3);
        Assert.Equal(40, box.TranslationX, 3);
    }

    [Fact]
    public void Nested_child_stagger_does_not_extend_parent_span()
    {
        var child = Motion.None.FadeIn().WithDuration(300).Stagger(100);
        var parent = Motion.None.FadeOut().WithDuration(300).Add(child);
        Assert.Equal(300u, parent.Duration);
    }

    [Fact]
    public void Named_spans_match_add_then_windows()
    {
        var intro = Motion.None.FadeIn().WithDuration(200).WithEasing(Easing.Linear);
        var pulse = Motion.None.Opacity(1, 0).WithDuration(200).WithEasing(Easing.Linear);
        var motion = Motion.None.WithDuration(200)
            .Add(intro, 0, "intro")
            .Then(pulse, "pulse");
        var player = MotionPlayer.Create(motion, [new BoxView()], new MotionClock(manual: true));

        Assert.Equal(400u, player.Duration);
        Assert.True(player.TrySpan("intro", out var introBegin, out var introEnd));
        Assert.True(player.TrySpan("pulse", out var pulseBegin, out var pulseEnd));
        Assert.Equal(0, introBegin, 3);
        Assert.Equal(0.5, introEnd, 3);
        Assert.Equal(0.5, pulseBegin, 3);
        Assert.Equal(1, pulseEnd, 3);
        Assert.False(player.TrySpan("missing", out _, out _));
    }

    [Fact]
    public void Duplicate_id_last_wins()
    {
        var fade = Motion.None.FadeIn().WithDuration(100).WithEasing(Easing.Linear);
        var slide = Motion.None.TranslateX(0, 40).WithDuration(100).WithEasing(Easing.Linear);
        var motion = Motion.None.WithDuration(100)
            .Add(fade, 0, "beat")
            .Then(slide, "beat");
        var player = MotionPlayer.Create(motion, [new BoxView()], new MotionClock(manual: true));

        Assert.True(player.TrySpan("beat", out var begin, out var end));
        Assert.Equal(0.5, begin, 3);
        Assert.Equal(1, end, 3);
    }

    [Fact]
    public void Seek_id_jumps_to_child_start()
    {
        var box = new BoxView { Opacity = 1 };
        var intro = Motion.None.FadeIn().WithDuration(200).WithEasing(Easing.Linear);
        var pulse = Motion.None.Opacity(1, 0).WithDuration(200).WithEasing(Easing.Linear);
        var player = MotionPlayer.Create(
            Motion.None.WithDuration(200).WithEasing(Easing.Linear).Add(intro, 0, "intro").Then(pulse, "pulse"),
            [box],
            new MotionClock(manual: true));

        player.Seek("pulse");
        Assert.Equal(1, box.Opacity, 3);
        Assert.Equal(0.5, player.Progress, 3);
    }

    [Fact]
    public void Named_span_maps_through_stagger_player_span()
    {
        var motion = Motion.None.WithDuration(300)
            .Add(Motion.None.FadeIn().WithDuration(300), 0, "intro")
            .Stagger(100);
        var player = MotionPlayer.Create(
            motion,
            [new BoxView(), new BoxView()],
            new MotionClock(manual: true));

        Assert.Equal(400u, player.Duration);
        Assert.True(player.TrySpan("intro", out var begin, out var end));
        Assert.Equal(0, begin, 3);
        Assert.Equal(0.75, end, 3);
    }

    [Fact]
    public void Add_bakes_a_child_delay_and_keeps_the_parent_hold()
    {
        var box = new BoxView { Opacity = 1, TranslationX = 0 };
        var clock = new MotionClock(manual: true);
        var child = Motion.None.TranslateX(0, 40).WithDuration(300).WithEasing(Easing.Linear).WithDelay(100);
        var parent = Motion.None.FadeIn().WithDuration(300).WithEasing(Easing.Linear).Add(child);
        var held = parent.WithDelay(50);

        Assert.Equal(0u, parent.Delay);
        Assert.Equal(400u, parent.Duration);
        Assert.Equal(50u, held.Delay);
        Assert.Equal(400u, held.Duration);

        var player = MotionPlayer.Create(parent, [box], clock);
        Assert.Equal(400u, player.Duration);
        _ = player.ForwardAsync();
        clock.Tick(100);
        Assert.Equal(0, box.TranslationX, 3);
        Assert.True(box.Opacity > 0);
        clock.Tick(200);
        Assert.Equal(1, box.Opacity, 3);
        Assert.True(box.TranslationX > 0);
        clock.Tick(100);
        Assert.Equal(40, box.TranslationX, 3);
    }

    [Fact]
    public void Then_places_a_delayed_child_after_the_span_plus_the_delay()
    {
        var box = new BoxView { Opacity = 1, TranslationX = 0 };
        var clock = new MotionClock(manual: true);
        var motion = Motion.None.FadeIn().WithDuration(200).WithEasing(Easing.Linear)
            .Then(Motion.None.TranslateX(0, 40).WithDuration(200).WithEasing(Easing.Linear).WithDelay(100));
        var player = MotionPlayer.Create(motion, [box], clock);

        Assert.Equal(500u, motion.Duration);
        Assert.Equal(0u, motion.Delay);

        _ = player.ForwardAsync();
        clock.Tick(200);
        Assert.Equal(1, box.Opacity, 3);
        Assert.Equal(0, box.TranslationX, 3);
        clock.Tick(100);
        Assert.Equal(0, box.TranslationX, 3);
        clock.Tick(200);
        Assert.Equal(40, box.TranslationX, 3);
    }

    [Fact]
    public void And_bakes_each_delay_and_the_result_delay_is_zero()
    {
        var box = new BoxView { Opacity = 1, TranslationX = 0 };
        var clock = new MotionClock(manual: true);
        var both = Motion.None.FadeIn().WithDuration(200).WithEasing(Easing.Linear).WithDelay(100)
            .And(Motion.None.TranslateX(0, 40).WithDuration(200).WithEasing(Easing.Linear));

        Assert.Equal(0u, both.Delay);
        Assert.Equal(300u, both.Duration);

        var player = MotionPlayer.Create(both, [box], clock);
        _ = player.ForwardAsync();
        clock.Tick(100);
        Assert.Equal(0, box.Opacity, 3);
        Assert.Equal(20, box.TranslationX, 3);
        clock.Tick(100);
        Assert.Equal(0.5, box.Opacity, 3);
        Assert.Equal(40, box.TranslationX, 3);
        clock.Tick(100);
        Assert.Equal(1, box.Opacity, 3);
        Assert.Equal(40, box.TranslationX, 3);
    }

    [Fact]
    public void WithDelay_on_an_and_result_holds_both_sides()
    {
        var box = new BoxView { Opacity = 1, TranslationX = 0 };
        var clock = new MotionClock(manual: true);
        var both = Motion.None.FadeIn().WithDuration(200).WithEasing(Easing.Linear)
            .And(Motion.None.TranslateX(0, 40).WithDuration(200).WithEasing(Easing.Linear))
            .WithDelay(100);

        Assert.Equal(100u, both.Delay);
        Assert.Equal(200u, both.Duration);

        var player = MotionPlayer.Create(both, [box], clock);
        Assert.Equal(200u, player.Duration);
        var task = player.ForwardAsync();
        clock.Tick(100);
        Assert.Equal(0, box.Opacity);
        Assert.Equal(0, box.TranslationX);
        Assert.False(task.IsCompleted);
        clock.Tick(200);
        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(1, box.Opacity, 3);
        Assert.Equal(40, box.TranslationX, 3);
    }
}
