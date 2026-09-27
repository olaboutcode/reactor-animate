using Microsoft.Maui.Controls.Shapes;
using Reactor.Animate.Animation;
using Reactor.Animate.Motion;
using Xunit;

namespace MauiAnimate.Tests;

public sealed class MotionPathTests
{
    [Fact]
    public void Linear_path_writes_translation()
    {
        var box = new BoxView();
        var clock = new MotionClock(manual: true);
        var geometry = new PathGeometry();
        var figure = new PathFigure { StartPoint = new Point(0, 0) };
        figure.Segments.Add(new LineSegment { Point = new Point(100, 0) });
        geometry.Figures.Add(figure);

        var player = MotionPlayer.Create(
            Motion.None.Path(geometry).WithDuration(200).WithEasing(Easing.Linear),
            [box],
            clock);

        _ = player.ForwardAsync();
        Assert.Equal(0, box.TranslationX, 3);
        Assert.Equal(0, box.TranslationY, 3);
        clock.Tick(100);
        Assert.Equal(50, box.TranslationX, 3);
        Assert.Equal(0, box.TranslationY, 3);
        clock.Tick(100);
        Assert.Equal(100, box.TranslationX, 3);
        Assert.Equal(0, box.TranslationY, 3);
    }

    [Fact]
    public void Arc_midpoint_is_on_the_arc_not_the_chord()
    {
        var box = new BoxView();
        var clock = new MotionClock(manual: true);
        var geometry = new PathGeometry();
        var figure = new PathFigure { StartPoint = new Point(100, 0) };
        figure.Segments.Add(new ArcSegment
        {
            Point = new Point(0, 100),
            Size = new Size(100, 100),
            SweepDirection = SweepDirection.Clockwise,
            IsLargeArc = false,
        });
        geometry.Figures.Add(figure);

        var player = MotionPlayer.Create(
            Motion.None.Path(geometry).WithDuration(200).WithEasing(Easing.Linear),
            [box],
            clock);

        _ = player.ForwardAsync();
        clock.Tick(100);
        Assert.True(box.TranslationX > 60, $"x={box.TranslationX}");
        Assert.True(box.TranslationY > 60, $"y={box.TranslationY}");
        clock.Tick(100);
        Assert.Equal(0, box.TranslationX, 1);
        Assert.Equal(100, box.TranslationY, 1);
    }
}
