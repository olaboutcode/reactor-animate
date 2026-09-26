namespace Reactor.Animate.Animation;

public enum SlideFrom
{
    Left,
    Right,
    Top,
    Bottom,
}

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
    {
        if (milliseconds == Duration)
            return this;

        var fullSpan = _tracks.Count == 0 || AllFullSpan(_tracks);
        if (fullSpan)
            return new Motion(milliseconds, Easing, _tracks);

        var span = Math.Max(Duration, milliseconds);
        if (span == Duration)
            return this;
        return new Motion(span, Easing, Rebase(_tracks, Duration, span));
    }

    public Motion WithEasing(Easing easing)
        => new(Duration, easing ?? throw new ArgumentNullException(nameof(easing)), _tracks);

    public Motion Opacity(double to)
        => Add(VisualElement.OpacityProperty, null, to);

    public Motion Opacity(double from, double to)
        => Add(VisualElement.OpacityProperty, from, to);

    /// <summary>
    /// View translation in device-independent pixels (to-only).
    /// Not <see cref="Transition.Translate"/> (FLIP invert extra).
    /// </summary>
    public Motion Translate(double x, double y)
        => TranslateX(x).TranslateY(y);

    /// <summary>
    /// View translation in device-independent pixels.
    /// Not <see cref="Transition.Translate"/> (FLIP invert extra).
    /// </summary>
    public Motion Translate(double fromX, double fromY, double toX, double toY)
        => TranslateX(fromX, toX).TranslateY(fromY, toY);

    public Motion TranslateX(double to)
        => Add(VisualElement.TranslationXProperty, null, to);

    public Motion TranslateX(double from, double to)
        => Add(VisualElement.TranslationXProperty, from, to);

    public Motion TranslateY(double to)
        => Add(VisualElement.TranslationYProperty, null, to);

    public Motion TranslateY(double from, double to)
        => Add(VisualElement.TranslationYProperty, from, to);

    /// <summary>
    /// Writes both <see cref="VisualElement.ScaleXProperty"/> and
    /// <see cref="VisualElement.ScaleYProperty"/> (not <c>ScaleProperty</c>).
    /// </summary>
    public Motion Scale(double to)
        => ScaleX(to).ScaleY(to);

    public Motion Scale(double from, double to)
        => ScaleX(from, to).ScaleY(from, to);

    public Motion ScaleX(double to)
        => Add(VisualElement.ScaleXProperty, null, to);

    public Motion ScaleX(double from, double to)
        => Add(VisualElement.ScaleXProperty, from, to);

    public Motion ScaleY(double to)
        => Add(VisualElement.ScaleYProperty, null, to);

    public Motion ScaleY(double from, double to)
        => Add(VisualElement.ScaleYProperty, from, to);

    /// <summary>
    /// View rotation in degrees. Not <see cref="Transition.Rotate"/> (FLIP invert extra).
    /// </summary>
    public Motion Rotate(double toDegrees)
        => Add(VisualElement.RotationProperty, null, toDegrees);

    public Motion Rotate(double fromDegrees, double toDegrees)
        => Add(VisualElement.RotationProperty, fromDegrees, toDegrees);

    public Motion BackgroundColor(Color to)
        => Add(VisualElement.BackgroundColorProperty, null, to);

    public Motion BackgroundColor(Color from, Color to)
        => Add(VisualElement.BackgroundColorProperty, from, to);

    public Motion Width(double to)
        => Add(VisualElement.WidthRequestProperty, null, to);

    public Motion Width(double from, double to)
        => Add(VisualElement.WidthRequestProperty, from, to);

    public Motion Height(double to)
        => Add(VisualElement.HeightRequestProperty, null, to);

    public Motion Height(double from, double to)
        => Add(VisualElement.HeightRequestProperty, from, to);

    public Motion CornerRadius(CornerRadius to)
        => AddSemantic(SemanticTrack.CornerRadius, null, to);

    public Motion CornerRadius(CornerRadius from, CornerRadius to)
        => AddSemantic(SemanticTrack.CornerRadius, from, to);

    public Motion Property(BindableProperty property, object to)
        => Add(property ?? throw new ArgumentNullException(nameof(property)), null, to);

    public Motion Property(BindableProperty property, object from, object to)
        => Add(property ?? throw new ArgumentNullException(nameof(property)), from, to);

    public Motion FadeIn()
        => Opacity(0, 1);

    public Motion FadeOut()
        => Opacity(1, 0);

    public Motion SlideIn(SlideFrom from, double distance = 24)
        => from switch
        {
            SlideFrom.Left => TranslateX(-distance, 0),
            SlideFrom.Right => TranslateX(distance, 0),
            SlideFrom.Top => TranslateY(-distance, 0),
            SlideFrom.Bottom => TranslateY(distance, 0),
            _ => TranslateX(-distance, 0),
        };

    public Motion SlideOut(SlideFrom to, double distance = 24)
        => to switch
        {
            SlideFrom.Left => TranslateX(0, -distance),
            SlideFrom.Right => TranslateX(0, distance),
            SlideFrom.Top => TranslateY(0, -distance),
            SlideFrom.Bottom => TranslateY(0, distance),
            _ => TranslateX(0, -distance),
        };

    public Motion ScaleIn(double from = 0.85)
        => Scale(from, 1);

    public Motion ScaleOut(double to = 0.85)
        => Scale(1, to);

    /// <summary>
    /// Parallel merge. Parent span is <c>max(left, right)</c>. Tracks are rebased
    /// in milliseconds and are not stretched. Later tracks win on overlap.
    /// </summary>
    public static Motion operator |(Motion left, Motion right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var span = Math.Max(left.Duration, right.Duration);
        var easing = !ReferenceEquals(right.Easing, Timing.MotionEasing)
            ? right.Easing
            : left.Easing;
        var tracks = Concat(Rebase(left._tracks, left.Duration, span), Rebase(right._tracks, right.Duration, span));
        return new Motion(span, easing, tracks);
    }

    public MotionPlayer Bind(params VisualElement[] targets)
        => MotionPlayer.Create(this, targets);

    public MotionPlayer Bind(IEnumerable<VisualElement> targets)
        => MotionPlayer.Create(this, targets as IReadOnlyList<VisualElement> ?? [.. targets]);

    Motion Add(BindableProperty property, object? from, object to)
        => Append(new MotionTrack(property, SemanticTrack.None, from, to, 0, 1, null, null));

    Motion AddSemantic(SemanticTrack semantic, object? from, object to)
        => Append(new MotionTrack(null, semantic, from, to, 0, 1, null, null));

    Motion Append(MotionTrack track)
    {
        var tracks = new MotionTrack[_tracks.Count + 1];
        for (var i = 0; i < _tracks.Count; i++)
            tracks[i] = _tracks[i];
        tracks[_tracks.Count] = track;
        return new Motion(Duration, Easing, tracks);
    }

    static bool AllFullSpan(IReadOnlyList<MotionTrack> tracks)
    {
        for (var i = 0; i < tracks.Count; i++)
        {
            if (tracks[i].Begin != 0 || tracks[i].End != 1)
                return false;
        }

        return true;
    }

    static IReadOnlyList<MotionTrack> Rebase(IReadOnlyList<MotionTrack> tracks, uint fromSpan, uint toSpan)
    {
        if (fromSpan == toSpan || toSpan == 0 || tracks.Count == 0)
            return tracks;

        var scale = (double)fromSpan / toSpan;
        var rebased = new MotionTrack[tracks.Count];
        for (var i = 0; i < tracks.Count; i++)
        {
            var track = tracks[i];
            rebased[i] = track with
            {
                Begin = track.Begin * scale,
                End = track.End * scale,
            };
        }

        return rebased;
    }

    static IReadOnlyList<MotionTrack> Concat(
        IReadOnlyList<MotionTrack> left,
        IReadOnlyList<MotionTrack> right)
    {
        if (left.Count == 0)
            return right;
        if (right.Count == 0)
            return left;

        var tracks = new MotionTrack[left.Count + right.Count];
        for (var i = 0; i < left.Count; i++)
            tracks[i] = left[i];
        for (var i = 0; i < right.Count; i++)
            tracks[left.Count + i] = right[i];
        return tracks;
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
