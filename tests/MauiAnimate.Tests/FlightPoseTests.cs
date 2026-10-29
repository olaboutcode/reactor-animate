using Reactor.Animate.Internals;
using Xunit;

namespace MauiAnimate.Tests;

public sealed class FlightPoseTests
{
    static readonly Rect From = new(10, 20, 100, 40);

    static readonly Rect To = new(30, 80, 200, 120);

    [Fact]
    public void Start_is_the_source_frame_and_end_is_the_destination()
    {
        var start = FlightPose.At(From, To, default, restRotation: 0, progress: 0);
        var end = FlightPose.At(From, To, default, restRotation: 0, progress: 1);

        Assert.Equal(10, start.X);
        Assert.Equal(20, start.Y);
        Assert.Equal(100, start.Width);
        Assert.Equal(40, start.Height);
        Assert.Equal(30, end.X);
        Assert.Equal(80, end.Y);
        Assert.Equal(200, end.Width);
        Assert.Equal(120, end.Height);
        Assert.Equal(0, end.Rotation);
    }

    [Fact]
    public void Anchor_does_not_move_the_frame()
    {
        var corner = FlightPose.At(From, To, new FlipExtras(0, 0, 0, 0, 0), 0, 0.5);
        var center = FlightPose.At(From, To, new FlipExtras(0.5, 0.5, 0, 0, 0), 0, 0.5);

        Assert.Equal(corner.X, center.X);
        Assert.Equal(corner.Y, center.Y);
        Assert.Equal(corner.Width, center.Width);
        Assert.Equal(corner.Height, center.Height);
        Assert.Equal(20, corner.X);
        Assert.Equal(50, corner.Y);
        Assert.Equal(150, corner.Width);
        Assert.Equal(80, corner.Height);
        Assert.Equal(0.5, center.AnchorX);
        Assert.Equal(0, corner.AnchorX);
    }

    [Fact]
    public void Rotation_and_nudge_ease_back_to_rest()
    {
        var extras = new FlipExtras(0.5, 0.5, 180, 8, -4);
        var start = FlightPose.At(From, To, extras, restRotation: 0, progress: 0);
        var end = FlightPose.At(From, To, extras, restRotation: 0, progress: 1);

        Assert.Equal(180, start.Rotation);
        Assert.Equal(18, start.X);
        Assert.Equal(16, start.Y);
        Assert.Equal(0, end.Rotation);
        Assert.Equal(30, end.X);
        Assert.Equal(80, end.Y);
    }

    [Fact]
    public void Negated_pop_rotation_starts_at_the_opposite_angle()
    {
        var popped = new FlipExtras(0.5, 0.5, 90, 0, 0).Negate();
        var start = FlightPose.At(To, From, popped, restRotation: 0, progress: 0);

        Assert.Equal(-90, start.Rotation);
        Assert.Equal(To.X, start.X);
        Assert.Equal(To.Width, start.Width);
    }
}
