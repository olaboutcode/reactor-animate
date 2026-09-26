using Reactor.Animate.Animation;
using Reactor.Animate.Motion;
using Xunit;

namespace MauiAnimate.Tests;

public sealed class FlipClipTests
{
    [Fact]
    public void Linear_opacity_plays_on_motion_clock()
    {
        var box = new BoxView { Opacity = 0 };
        var clock = new MotionClock(manual: true);
        var clip = FlipTween.On(box)
            .Duration(100)
            .Easing(Easing.Linear)
            .To(VisualElement.OpacityProperty, 1d, 0d)
            .Build(clock);

        var task = clip.PlayAsync();
        Assert.Equal(0, box.Opacity);
        clock.Tick(50);
        Assert.Equal(0.5, box.Opacity, 3);
        clock.Tick(50);
        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(1, box.Opacity);
    }

    [Fact]
    public void Delay_holds_from_until_begin()
    {
        var box = new BoxView { Opacity = 1 };
        var clock = new MotionClock(manual: true);
        var clip = FlipTween.On(box)
            .Duration(100)
            .Easing(Easing.Linear)
            .Delay(0.7)
            .To(VisualElement.OpacityProperty, 1d, 0d)
            .Build(clock);

        var task = clip.PlayAsync();
        clock.Tick(69);
        Assert.Equal(0, box.Opacity);
        clock.Tick(31);
        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(1, box.Opacity);
    }
}
