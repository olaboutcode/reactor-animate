namespace Reactor.Animate;

/// <summary>
/// How <see cref="Motion.BackgroundColor(Color)"/> blends between colors.
/// </summary>
public enum ColorSpace
{
    /// <summary>Shortest hue arc. Alpha stays linear. This is the default.</summary>
    Hsv,

    /// <summary>Each channel, including alpha, in a straight line.</summary>
    Rgb,
}
