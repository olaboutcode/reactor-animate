using Microsoft.Maui.Controls.Shapes;

namespace Reactor.Animate.Internals;

// Named child window on a motion timeline. Begin and End are fractions of the parent span.
internal readonly record struct NamedSpan(string Id, double Begin, double End);

// Channels that are not a BindableProperty. None means the track writes Property.
internal enum SemanticTrack
{
    None,
    CornerRadius,
    Path,
}

// One channel of a Motion recipe. Begin and End are fractions of that motion.
// A null From is captured from the view on the first forward.
internal readonly record struct MotionTrack(
    BindableProperty? Property,
    SemanticTrack Semantic,
    object? From,
    object To,
    double Begin,
    double End,
    Easing? Easing,
    IReadOnlyList<MotionKeyframe>? Keyframes)
{
    public PathGeometry? Path { get; init; }

    public double PathFrom { get; init; }

    public double PathTo { get; init; } = 1;
}

// One stop on a track. Offset is 0–1 of that track. Easing is the curve into the stop.
internal readonly record struct MotionKeyframe(
    double Offset,
    object Value,
    Easing? Easing);
