namespace Reactor.Animate.Internals;

/// <summary>
/// Extra anchor, rotation, and translation on top of the frame morph.
/// <see cref="FlipExtras.Negate"/> is what a pop plays. <see cref="FlipExtras.Merge"/>
/// keeps a later value when it is not zero, so a shared default does not wipe a per-tag extra.
/// </summary>
internal readonly record struct FlipExtras(
    double AnchorX,
    double AnchorY,
    double Rotation,
    double TranslationX,
    double TranslationY)
{
    public FlipExtras Negate()
        => this with
        {
            Rotation = -Rotation,
            TranslationX = -TranslationX,
            TranslationY = -TranslationY,
        };

    public static FlipExtras Merge(FlipExtras left, FlipExtras right)
        => new(
            AnchorX: right.AnchorX != 0 || right.AnchorY != 0 ? right.AnchorX : left.AnchorX,
            AnchorY: right.AnchorX != 0 || right.AnchorY != 0 ? right.AnchorY : left.AnchorY,
            Rotation: right.Rotation != 0 ? right.Rotation : left.Rotation,
            TranslationX: right.TranslationX != 0 ? right.TranslationX : left.TranslationX,
            TranslationY: right.TranslationY != 0 ? right.TranslationY : left.TranslationY);
}
