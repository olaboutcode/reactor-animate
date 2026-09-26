namespace Reactor.Animate.Animation;

/// <summary>
/// Mass-spring-damper. Not an <see cref="Easing"/>. Playback lasts until rest.
/// </summary>
public readonly record struct Spring(double Stiffness, double Damping, double Mass)
{
    public static Spring Default { get; } = new(180, 16, 1);

    public static Spring Snappy { get; } = new(400, 22, 1);

    public static Spring Gentle { get; } = new(90, 18, 1);

    public Spring()
        : this(180, 16, 1)
    {
    }
}
