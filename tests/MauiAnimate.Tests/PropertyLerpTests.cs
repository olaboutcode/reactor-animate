using Reactor.Animate;
using Reactor.Animate.Internals;
using Xunit;

namespace MauiAnimate.Tests;

public sealed class PropertyLerpTests
{
    [Fact]
    public void Hsv_red_to_lime_does_not_pass_through_mud()
    {
        var hsv = PropertyLerp.LerpHsv(Colors.Red, Colors.Lime, 0.5);
        var rgb = PropertyLerp.LerpRgb(Colors.Red, Colors.Lime, 0.5);

        Assert.True(hsv.Blue < 0.15);
        Assert.True(hsv.Red > 0.4);
        Assert.True(hsv.Green > 0.4);
        Assert.True(rgb.Red + rgb.Green + rgb.Blue < hsv.Red + hsv.Green + hsv.Blue);
    }

    [Fact]
    public void Alpha_lerps_linearly_in_hsv()
    {
        var from = Colors.Red.WithAlpha(0);
        var to = Colors.Red.WithAlpha(1);
        var mid = PropertyLerp.LerpHsv(from, to, 0.5);
        Assert.Equal(0.5, mid.Alpha, 3);
    }

    [Fact]
    public void Rgb_opt_in_matches_channel_midpoint()
    {
        var mid = (Color)PropertyLerp.Lerp(Colors.Red, Colors.Lime, 0.5, ColorSpace.Rgb)!;
        Assert.Equal(0.5, mid.Red, 3);
        Assert.Equal(0.5, mid.Green, 3);
        Assert.Equal(0, mid.Blue, 3);
    }
}
