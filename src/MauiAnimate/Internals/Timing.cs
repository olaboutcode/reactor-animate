using Reactor.Animate;

namespace Reactor.Animate.Internals;

// Default timing. Page flights are 400 ms CubicOut. Motions are 300 ms CubicOut.
internal static class Timing
{
    public const uint PageDuration = 400;

    public static Easing PageEasing { get; } = Easing.CubicOut;

    public const uint MotionDuration = 300;

    public static Easing MotionEasing { get; } = Easing.CubicOut;
}
