namespace Reactor.Animate.Animation;

/// <summary>
/// Immutable in-page motion recipe. Holds no targets. Bind at play time.
/// </summary>
public sealed class Motion
{
    public static Motion None { get; } = new(Timing.MotionDuration, Timing.MotionEasing, []);

    readonly IReadOnlyList<MotionTrack> _tracks;

    Motion(uint duration, Easing easing, IReadOnlyList<MotionTrack> tracks)
    {
        Duration = duration;
        Easing = easing;
        _tracks = tracks;
    }

    public uint Duration { get; }

    public Easing Easing { get; }

    internal IReadOnlyList<MotionTrack> Tracks => _tracks;

    public Motion WithDuration(uint milliseconds)
        => new(milliseconds, Easing, _tracks);

    public Motion WithEasing(Easing easing)
        => new(Duration, easing ?? throw new ArgumentNullException(nameof(easing)), _tracks);

    public Motion Opacity(double to)
        => Add(VisualElement.OpacityProperty, null, to);

    public Motion Opacity(double from, double to)
        => Add(VisualElement.OpacityProperty, from, to);

    public MotionPlayer Bind(params VisualElement[] targets)
        => MotionPlayer.Create(this, targets);

    public MotionPlayer Bind(IEnumerable<VisualElement> targets)
        => MotionPlayer.Create(this, targets as IReadOnlyList<VisualElement> ?? [.. targets]);

    Motion Add(BindableProperty property, object? from, object to)
    {
        var tracks = new MotionTrack[_tracks.Count + 1];
        for (var i = 0; i < _tracks.Count; i++)
            tracks[i] = _tracks[i];
        tracks[_tracks.Count] = new MotionTrack(
            property,
            SemanticTrack.None,
            from,
            to,
            Begin: 0,
            End: 1,
            Easing: null,
            Keyframes: null);
        return new Motion(Duration, Easing, tracks);
    }
}

internal enum SemanticTrack
{
    None,
    CornerRadius,
}

internal readonly record struct MotionTrack(
    BindableProperty? Property,
    SemanticTrack Semantic,
    object? From,
    object To,
    double Begin,
    double End,
    Easing? Easing,
    IReadOnlyList<MotionKeyframe>? Keyframes);

internal readonly record struct MotionKeyframe(
    double Offset,
    object Value,
    Easing? Easing);
