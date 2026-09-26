namespace Reactor.Animate.Animation;

internal static class Timing
{
    public const uint PageDuration = 400;

    public static Easing PageEasing { get; } = Easing.CubicOut;

    public const uint MotionDuration = 300;

    public static Easing MotionEasing { get; } = Easing.CubicOut;
}
