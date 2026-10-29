namespace Reactor.Animate.Internals;

/// <summary>
/// Where one flying hero is drawn at a point in the clip.
/// The rectangle is the lerp of the two window frames. Anchor cancels out of
/// that lerp and is only the rotation pivot. Scroll parents are not involved.
/// </summary>
internal static class FlightPose
{
    public readonly record struct Sample(
        double X,
        double Y,
        double Width,
        double Height,
        double Rotation,
        double AnchorX,
        double AnchorY);

    public static Sample At(Rect from, Rect to, FlipExtras extras, double restRotation, double progress)
    {
        var t = double.IsNaN(progress) ? 0 : Math.Clamp(progress, 0, 1);
        return new Sample(
            Lerp(from.X, to.X, t) + extras.TranslationX * (1 - t),
            Lerp(from.Y, to.Y, t) + extras.TranslationY * (1 - t),
            Math.Max(0, Lerp(from.Width, to.Width, t)),
            Math.Max(0, Lerp(from.Height, to.Height, t)),
            restRotation + extras.Rotation * (1 - t),
            extras.AnchorX,
            extras.AnchorY);
    }

    static double Lerp(double from, double to, double t)
        => from + (to - from) * t;
}

/// <summary>
/// One hero in a flight. <see cref="From"/> and <see cref="To"/> are window
/// bounds measured before anything moves. <see cref="StartLook"/> is the other
/// view, still in the tree. <see cref="StartPlate"/> is a picture taken before
/// that view was popped.
/// </summary>
internal sealed class FlightLeg
{
    public required VisualElement Flying { get; init; }

    public VisualElement? StartLook { get; init; }

    public FlightPlate? StartPlate { get; init; }

    public required Rect From { get; init; }

    public required Rect To { get; init; }

    public FlipExtras Extras { get; init; }

    public double RestRotation { get; init; }
}
