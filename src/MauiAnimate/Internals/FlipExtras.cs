namespace Reactor.Animate.Internals;

// Anchor, rotation, and translation added on top of the frame morph.
// Negate is the pop. Merge keeps a later non-zero value.
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
