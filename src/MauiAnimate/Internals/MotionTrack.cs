using Microsoft.Maui.Controls.Shapes;

namespace Reactor.Animate.Internals;

internal readonly record struct NamedSpan(string Id, double Begin, double End);

internal enum SemanticTrack
{
    None,
    CornerRadius,
    Path,
}

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

internal readonly record struct MotionKeyframe(
    double Offset,
    object Value,
    Easing? Easing);
