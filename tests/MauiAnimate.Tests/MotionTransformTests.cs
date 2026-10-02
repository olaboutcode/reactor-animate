using Reactor.Animate;
using Reactor.Animate.Internals;
using Xunit;

namespace MauiAnimate.Tests;

public sealed class MotionTransformTests
{
    [Fact]
    public void SetEntry_3_2_is_the_perspective_cell()
    {
        var matrix = Matrix4.Identity.SetEntry(3, 2, 0.002);

        Assert.Equal(0.002, matrix.Entry(3, 2), 6);
        Assert.Equal(1, matrix.Entry(0, 0), 6);
        Assert.Equal(-0.002, MatrixMaps.RowMajor(matrix, 2, 3), 6);
    }

    [Fact]
    public void RotateY_then_translate_matches_flutter_order()
    {
        var matrix = Matrix4.Identity
            .SetEntry(3, 2, 0.002)
            .RotateY(Math.PI / 2)
            .Translate(0, 50);

        var pose = MatrixPose.From(matrix);
        Assert.Equal(0, pose.TranslationX, 3);
        Assert.Equal(50, pose.TranslationY, 3);
        Assert.Equal(90, pose.RotationY, 3);
        Assert.Equal(0, pose.RotationX, 3);
        Assert.Equal(0, pose.RotationZ, 3);
        Assert.Equal(0.002, pose.Perspective, 6);
        Assert.Equal(1, pose.ScaleX, 3);
    }

    [Fact]
    public void Half_turn_stays_on_the_y_axis()
    {
        var pose = MatrixPose.From(Matrix4.Identity.RotateY(Math.PI));

        Assert.Equal(180, pose.RotationY, 3);
        Assert.Equal(0, pose.RotationX, 3);
        Assert.Equal(0, pose.RotationZ, 3);
    }

    [Fact]
    public void Scale_multiplies_both_axes()
    {
        var pose = MatrixPose.From(Matrix4.Identity.Scale(1.25));

        Assert.Equal(1.25, pose.ScaleX, 3);
        Assert.Equal(1.25, pose.ScaleY, 3);
    }

    [Fact]
    public void Transform_writes_the_matrix_for_eased_progress()
    {
        var box = new BoxView();
        var clock = new MotionClock(manual: true);
        var player = MotionPlayer.Create(
            Motion.None
                .Transform(t => Matrix4.Identity
                    .SetEntry(3, 2, 0.002)
                    .RotateY(t * Math.PI)
                    .Translate(0, t * 50))
                .WithDuration(200)
                .WithEasing(Easing.Linear),
            [box],
            clock);

        _ = player.ForwardAsync();
        Assert.Equal(0, box.TranslationY, 3);
        Assert.Equal(0, box.RotationY, 3);

        clock.Tick(100);
        Assert.Equal(25, box.TranslationY, 3);
        Assert.Equal(90, box.RotationY, 3);

        clock.Tick(100);
        Assert.Equal(50, box.TranslationY, 3);
        Assert.Equal(180, box.RotationY, 3);

        player.Reset();
        Assert.Equal(0, box.TranslationY, 3);
        Assert.Equal(0, box.RotationY, 3);
    }

    [Fact]
    public void Transform_rejects_a_null_callback()
    {
        Assert.Throws<ArgumentNullException>(() => Motion.None.Transform(null!));
    }
}
