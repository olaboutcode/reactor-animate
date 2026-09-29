using Reactor.Animate.Animation;
using Reactor.Animate.Motion;
using Xunit;

namespace MauiAnimate.Tests;

public sealed class MotionPerspectiveTests
{
    [Fact]
    public void RotateY_writes_degrees()
    {
        var box = new BoxView { RotationY = 10 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.RotateY(0, 60).WithDuration(300).WithEasing(Easing.Linear),
            [box],
            clock);

        _ = player.ForwardAsync();
        Assert.Equal(0, box.RotationY);
        clock.Tick(150);
        Assert.Equal(30, box.RotationY, 3);
        clock.Tick(150);
        Assert.Equal(60, box.RotationY, 3);
    }

    [Fact]
    public void RotateX_writes_degrees()
    {
        var box = new BoxView { RotationX = 5 };
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None.Perspective(0.001).RotateX(0, 40).WithDuration(200).WithEasing(Easing.Linear),
            [box],
            clock);

        _ = player.ForwardAsync();
        Assert.Equal(0, box.RotationX);
        clock.Tick(200);
        Assert.Equal(40, box.RotationX, 3);
        Assert.Equal(0.001, player.Motion.PerspectiveEntry);
    }

    [Fact]
    public void Far_edge_shrinks_and_near_edge_grows()
    {
        const double half = 100;
        const double entry = 0.001;

        var far = PerspectivePlane.Project(-half, half, 0, 45, entry);
        var near = PerspectivePlane.Project(half, half, 0, 45, entry);

        Assert.Equal(0.934, far.Y / half, 3);
        Assert.Equal(1.076, near.Y / half, 3);
        Assert.True(far.Y < half);
        Assert.True(near.Y > half);
    }

    [Fact]
    public void Android_camera_distance_matches_the_dip_eye_distance()
    {
        // entry 0.004 is a camera 250 dips away. SetCameraDistance divides by
        // densityDpi, so the argument is density² × eye × √5.
        const float density = 3f;
        var distance = PerspectivePlane.AndroidCameraDistance(0.004, density);
        var expected = density * density * (1.0 / 0.004) * Math.Sqrt(5.0);

        Assert.Equal(expected, distance, 3);
        Assert.True(distance > density * (1f / 0.004f));
    }

    [Fact]
    public void Zero_perspective_keeps_both_edges_the_same_height()
    {
        var far = PerspectivePlane.Project(-100, 40, 0, 45, 0);
        var near = PerspectivePlane.Project(100, 40, 0, 45, 0);

        Assert.Equal(40, far.Y, 6);
        Assert.Equal(40, near.Y, 6);
    }

    [Fact]
    public void Perspective_merges_and_follows_then()
    {
        var kept = Motion.None.Perspective(0.002).RotateY(0, 10)
            | Motion.None.RotateX(0, 5);
        Assert.Equal(0.002, kept.PerspectiveEntry);

        var replaced = kept | Motion.None.Perspective(0.004);
        Assert.Equal(0.004, replaced.PerspectiveEntry);

        var sequenced = Motion.None.RotateY(0, 20).WithDuration(100)
            .Then(Motion.None.Perspective(0.003).RotateY(20, 0).WithDuration(100));
        Assert.Equal(0.003, sequenced.PerspectiveEntry);
    }
}
