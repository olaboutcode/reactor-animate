using Reactor.Animate;
using Reactor.Animate.Internals;
using Xunit;

namespace MauiAnimate.Tests;

public sealed class MotionHelpersTests
{
    [Fact]
    public void Scale_writes_both_axes()
    {
        var box = new BoxView { ScaleX = 1, ScaleY = 1 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.Scale(0.5, 1).WithDuration(300).WithEasing(Easing.Linear),
            [box],
            clock);

        _ = player.ForwardAsync();
        Assert.Equal(0.5, box.ScaleX);
        Assert.Equal(0.5, box.ScaleY);
        clock.Tick(300);
        Assert.Equal(1, box.ScaleX);
        Assert.Equal(1, box.ScaleY);
    }

    [Fact]
    public void Background_midpoint_matches_hsv_lerp()
    {
        var box = new BoxView { BackgroundColor = Colors.Red };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.BackgroundColor(Colors.Red, Colors.Lime)
                .WithDuration(100)
                .WithEasing(Easing.Linear),
            [box],
            clock);

        _ = player.ForwardAsync();
        clock.Tick(50);

        var expected = PropertyLerp.LerpHsv(Colors.Red, Colors.Lime, 0.5);
        Assert.Equal(expected.Red, box.BackgroundColor.Red, 3);
        Assert.Equal(expected.Green, box.BackgroundColor.Green, 3);
        Assert.Equal(expected.Blue, box.BackgroundColor.Blue, 3);
    }

    [Fact]
    public void Background_rgb_midpoint_stays_on_the_channels()
    {
        var box = new BoxView { BackgroundColor = Colors.Red };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.BackgroundColor(Colors.Red, Colors.Lime)
                .WithColorSpace(ColorSpace.Rgb)
                .WithDuration(100)
                .WithEasing(Easing.Linear),
            [box],
            clock);

        _ = player.ForwardAsync();
        clock.Tick(50);

        Assert.Equal(0.5, box.BackgroundColor.Red, 3);
        Assert.Equal(0.5, box.BackgroundColor.Green, 3);
        Assert.Equal(0, box.BackgroundColor.Blue, 3);
    }

    [Fact]
    public void Background_keyframe_midpoint_matches_hsv_lerp()
    {
        var box = new BoxView { BackgroundColor = Colors.Blue };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.BackgroundColor(k => k.At(0, Colors.Red).At(1, Colors.Lime))
                .WithDuration(100)
                .WithEasing(Easing.Linear),
            [box],
            clock);

        _ = player.ForwardAsync();
        clock.Tick(50);

        var expected = PropertyLerp.LerpHsv(Colors.Red, Colors.Lime, 0.5);
        Assert.Equal(expected.Red, box.BackgroundColor.Red, 3);
        Assert.Equal(expected.Green, box.BackgroundColor.Green, 3);
        Assert.Equal(expected.Blue, box.BackgroundColor.Blue, 3);
    }

    [Fact]
    public void FadeIn_runs_opacity_0_to_1()
    {
        var box = new BoxView { Opacity = 1 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.FadeIn().WithDuration(300).WithEasing(Easing.Linear),
            [box],
            clock);

        _ = player.ForwardAsync();
        Assert.Equal(0, box.Opacity);
        clock.Tick(300);
        Assert.Equal(1, box.Opacity);
    }

    [Fact]
    public void Merge_rebases_to_max_span_without_stretching()
    {
        var box = new BoxView { Opacity = 1, TranslationX = 0 };
        var clock = new MotionClock(manual: true);
        var motion =
            Motion.None.FadeIn().WithDuration(200).WithEasing(Easing.Linear)
            .And(Motion.None.TranslateX(0, 100).WithDuration(400).WithEasing(Easing.Linear));
        var player = MotionPlayer.Create(motion, [box], clock);

        Assert.Equal(400u, motion.Duration);
        Assert.Equal(400u, player.Duration);

        _ = player.ForwardAsync();
        clock.Tick(200);
        Assert.Equal(1, box.Opacity);
        Assert.Equal(50, box.TranslationX, 3);

        clock.Tick(200);
        Assert.Equal(1, box.Opacity);
        Assert.Equal(100, box.TranslationX, 3);
        Assert.Equal(MotionPlaybackStatus.Completed, player.Status);
    }

    [Fact]
    public void CornerRadius_resolves_on_BoxView()
    {
        var box = new BoxView { CornerRadius = 4 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.CornerRadius(new CornerRadius(4), new CornerRadius(16))
                .WithDuration(300)
                .WithEasing(Easing.Linear),
            [box],
            clock);

        _ = player.ForwardAsync();
        clock.Tick(300);
        Assert.Equal(16, box.CornerRadius.TopLeft);
        Assert.Equal(16, box.CornerRadius.TopRight);
    }

    [Fact]
    public void Chained_opacity_and_translation_share_one_clock()
    {
        var box = new BoxView { Opacity = 1, TranslationY = 0 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.Opacity(0, 1).TranslateY(20, 0).WithDuration(300).WithEasing(Easing.Linear),
            [box],
            clock);

        _ = player.ForwardAsync();
        Assert.Equal(0, box.Opacity);
        Assert.Equal(20, box.TranslationY);
        clock.Tick(150);
        Assert.Equal(0.5, box.Opacity, 3);
        Assert.Equal(10, box.TranslationY, 3);
        clock.Tick(150);
        Assert.Equal(1, box.Opacity);
        Assert.Equal(0, box.TranslationY);
    }
}
