using Reactor.Animate;
using Reactor.Animate.Animation;
using Reactor.Animate.Motion;
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
}
