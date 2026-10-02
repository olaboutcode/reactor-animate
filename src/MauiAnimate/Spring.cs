namespace Reactor.Animate;

/// <summary>
/// Mass-spring-damper. Not an <see cref="Easing"/>. Playback lasts until rest.
/// </summary>
/// <param name="Stiffness">Spring constant. Higher reaches the target faster.</param>
/// <param name="Damping">Velocity damping. Higher settles with less overshoot.</param>
/// <param name="Mass">Mass. Higher moves more slowly.</param>
public readonly record struct Spring(double Stiffness, double Damping, double Mass)
{
    /// <summary>Stiffness 180, damping 16, mass 1.</summary>
    public static Spring Default { get; } = new(180, 16, 1);

    /// <summary>Stiffness 400, damping 22, mass 1.</summary>
    public static Spring Snappy { get; } = new(400, 22, 1);

    /// <summary>Stiffness 90, damping 18, mass 1.</summary>
    public static Spring Gentle { get; } = new(90, 18, 1);

    /// <summary>Same as <see cref="Default"/>.</summary>
    public Spring()
        : this(180, 16, 1)
    {
    }
}
