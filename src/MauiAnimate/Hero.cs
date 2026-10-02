namespace Reactor.Animate;

/// <summary>
/// Anchor, rotation, and translation for one shared-element tag.
/// The flight scales that view to the other frame around the anchor.
/// </summary>
public sealed class Hero
{
    internal FlipExtras Extras { get; }

    internal Hero(FlipExtras extras) => Extras = extras;

    /// <summary>
    /// Origin for the frame scale and rotation. <c>(0, 0)</c> is top-left, <c>(0.5, 0.5)</c> is center.
    /// </summary>
    public Hero Anchor(double x, double y)
        => new(Extras with { AnchorX = x, AnchorY = y });

    /// <summary>Origin for the frame scale and rotation, centered.</summary>
    public Hero AnchorCenter()
        => Anchor(0.5, 0.5);

    /// <summary>Origin for the frame scale and rotation at the top-left corner.</summary>
    public Hero AnchorTopLeft()
        => Anchor(0, 0);

    /// <summary>Origin for the frame scale and rotation at the top-right corner, <c>(1, 0)</c>.</summary>
    public Hero AnchorTopRight()
        => Anchor(1, 0);

    /// <summary>Origin for the frame scale and rotation at the bottom-left corner, <c>(0, 1)</c>.</summary>
    public Hero AnchorBottomLeft()
        => Anchor(0, 1);

    /// <summary>Origin for the frame scale and rotation at the bottom-right corner, <c>(1, 1)</c>.</summary>
    public Hero AnchorBottomRight()
        => Anchor(1, 1);

    /// <summary>
    /// Adds rotation to the invert, in degrees, then plays back to rest.
    /// Pop uses the negated angle.
    /// </summary>
    public Hero Rotate(double degrees)
        => new(Extras with { Rotation = degrees });

    /// <summary>
    /// Extra translation on the invert, in device-independent pixels.
    /// Pop uses the negated offset.
    /// </summary>
    public Hero Translate(double x, double y)
        => new(Extras with { TranslationX = x, TranslationY = y });
}
