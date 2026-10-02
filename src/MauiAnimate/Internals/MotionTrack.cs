using Microsoft.Maui.Controls.Shapes;

namespace Reactor.Animate.Internals;

/// <summary>
/// Named child window on a motion timeline. <c>Begin</c> and <c>End</c> are
/// fractions of the parent span. <c>Seek(id)</c> and <c>TrySpan</c> use the id.
/// </summary>
internal readonly record struct NamedSpan(string Id, double Begin, double End);

/// <summary>
/// Channels that are not a <see cref="BindableProperty"/>. <see cref="SemanticTrack.None"/>
/// means the track writes <see cref="MotionTrack.Property"/>. Corner radius and path
/// are sampled on their own because they are not a single bindable value.
/// </summary>
internal enum SemanticTrack
{
    None,
    CornerRadius,
    Path,
}

/// <summary>
/// One channel of a <see cref="Motion"/> recipe. <c>Begin</c> and <c>End</c> are
/// fractions of that motion. A null <c>From</c> is read from the view on the first
/// forward. Keyframes, when set, replace the single from/to pair.
/// </summary>
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

/// <summary>
/// One stop on a track. <c>Offset</c> is 0–1 of that track and must not move backward.
/// <c>Easing</c> is the curve into this stop. A null easing uses the motion easing.
/// </summary>
internal readonly record struct MotionKeyframe(
    double Offset,
    object Value,
    Easing? Easing);
