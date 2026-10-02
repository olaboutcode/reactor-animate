using Reactor.Animate;

namespace Reactor.Animate.Internals;

/// <summary>
/// Default timing shared by page flights and motions. A page flight is 400 ms
/// <see cref="Easing.CubicOut"/>. A motion is 300 ms <see cref="Easing.CubicOut"/>.
/// <see cref="HeroTransition.Merge"/> treats these defaults as unset, so a later
/// explicit duration or easing replaces them.
/// </summary>
internal static class Timing
{
    public const uint PageDuration = 400;

    public static Easing PageEasing { get; } = Easing.CubicOut;

    public const uint MotionDuration = 300;

    public static Easing MotionEasing { get; } = Easing.CubicOut;
}
