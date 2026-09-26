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
    public static Motion None { get; } = new(Timing.MotionDuration, Timing.MotionEasing, [], null);

    readonly IReadOnlyList<MotionTrack> _tracks;
    readonly Stagger? _stagger;

    Motion(uint duration, Easing easing, IReadOnlyList<MotionTrack> tracks, Stagger? stagger)
    {
        Duration = duration;
        Easing = easing;
        _tracks = tracks;
        _stagger = stagger;
    }

    public uint Duration { get; }

    public Easing Easing { get; }

    internal Stagger? StaggerSpec => _stagger;

    internal IReadOnlyList<MotionTrack> Tracks => _tracks;

    public Motion WithDuration(uint milliseconds)
    {
        if (milliseconds == Duration)
            return this;

        var fullSpan = _tracks.Count == 0 || AllFullSpan(_tracks);
        if (fullSpan)
            return new Motion(milliseconds, Easing, _tracks, _stagger);

        var span = Math.Max(Duration, milliseconds);
        if (span == Duration)
            return this;
        return new Motion(span, Easing, Rebase(_tracks, Duration, span), _stagger);
    }

    public Motion WithEasing(Easing easing)
        => new(Duration, easing ?? throw new ArgumentNullException(nameof(easing)), _tracks, _stagger);

    public Motion Opacity(double to)
        => Add(VisualElement.OpacityProperty, null, to);

    public Motion Opacity(double from, double to)
        => Add(VisualElement.OpacityProperty, from, to);

    public Motion Opacity(Func<KeyframeBuilder<double>, KeyframeBuilder<double>> frames)
        => AddKeyframes(VisualElement.OpacityProperty, SemanticTrack.None, frames);

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

    public Motion TranslateX(Func<KeyframeBuilder<double>, KeyframeBuilder<double>> frames)
        => AddKeyframes(VisualElement.TranslationXProperty, SemanticTrack.None, frames);

    public Motion TranslateY(double to)
        => Add(VisualElement.TranslationYProperty, null, to);

    public Motion TranslateY(double from, double to)
        => Add(VisualElement.TranslationYProperty, from, to);

    public Motion TranslateY(Func<KeyframeBuilder<double>, KeyframeBuilder<double>> frames)
        => AddKeyframes(VisualElement.TranslationYProperty, SemanticTrack.None, frames);

    /// <summary>
    /// Writes both <see cref="VisualElement.ScaleXProperty"/> and
    /// <see cref="VisualElement.ScaleYProperty"/> (not <c>ScaleProperty</c>).
    /// </summary>
    public Motion Scale(double to)
        => ScaleX(to).ScaleY(to);

    public Motion Scale(double from, double to)
        => ScaleX(from, to).ScaleY(from, to);

    public Motion Scale(Func<KeyframeBuilder<double>, KeyframeBuilder<double>> frames)
        => AddKeyframesBoth(VisualElement.ScaleXProperty, VisualElement.ScaleYProperty, frames);

    public Motion ScaleX(double to)
        => Add(VisualElement.ScaleXProperty, null, to);

    public Motion ScaleX(double from, double to)
        => Add(VisualElement.ScaleXProperty, from, to);

    public Motion ScaleX(Func<KeyframeBuilder<double>, KeyframeBuilder<double>> frames)
        => AddKeyframes(VisualElement.ScaleXProperty, SemanticTrack.None, frames);

    public Motion ScaleY(double to)
        => Add(VisualElement.ScaleYProperty, null, to);

    public Motion ScaleY(double from, double to)
        => Add(VisualElement.ScaleYProperty, from, to);

    public Motion ScaleY(Func<KeyframeBuilder<double>, KeyframeBuilder<double>> frames)
        => AddKeyframes(VisualElement.ScaleYProperty, SemanticTrack.None, frames);

    /// <summary>
    /// View rotation in degrees. Not <see cref="Transition.Rotate"/> (FLIP invert extra).
    /// </summary>
    public Motion Rotate(double toDegrees)
        => Add(VisualElement.RotationProperty, null, toDegrees);

    public Motion Rotate(double fromDegrees, double toDegrees)
        => Add(VisualElement.RotationProperty, fromDegrees, toDegrees);

    public Motion Rotate(Func<KeyframeBuilder<double>, KeyframeBuilder<double>> frames)
        => AddKeyframes(VisualElement.RotationProperty, SemanticTrack.None, frames);

    public Motion BackgroundColor(Color to)
        => Add(VisualElement.BackgroundColorProperty, null, to);

    public Motion BackgroundColor(Color from, Color to)
        => Add(VisualElement.BackgroundColorProperty, from, to);

    public Motion BackgroundColor(Func<KeyframeBuilder<Color>, KeyframeBuilder<Color>> frames)
        => AddKeyframes(VisualElement.BackgroundColorProperty, SemanticTrack.None, frames);

    public Motion Width(double to)
        => Add(VisualElement.WidthRequestProperty, null, to);

    public Motion Width(double from, double to)
        => Add(VisualElement.WidthRequestProperty, from, to);

    public Motion Width(Func<KeyframeBuilder<double>, KeyframeBuilder<double>> frames)
        => AddKeyframes(VisualElement.WidthRequestProperty, SemanticTrack.None, frames);

    public Motion Height(double to)
        => Add(VisualElement.HeightRequestProperty, null, to);

    public Motion Height(double from, double to)
        => Add(VisualElement.HeightRequestProperty, from, to);

    public Motion Height(Func<KeyframeBuilder<double>, KeyframeBuilder<double>> frames)
        => AddKeyframes(VisualElement.HeightRequestProperty, SemanticTrack.None, frames);

    public Motion CornerRadius(CornerRadius to)
        => AddSemantic(SemanticTrack.CornerRadius, null, to);

    public Motion CornerRadius(CornerRadius from, CornerRadius to)
        => AddSemantic(SemanticTrack.CornerRadius, from, to);

    public Motion CornerRadius(Func<KeyframeBuilder<CornerRadius>, KeyframeBuilder<CornerRadius>> frames)
        => AddKeyframes(null, SemanticTrack.CornerRadius, frames);

    /// <summary>
    /// Whole-state keyframes at 0–1 offsets of this motion. Omitted properties
    /// hold their last keyframed value; they do not lerp back to implicit from.
    /// Sequential millisecond steps belong on <c>Then</c>, not here.
    /// </summary>
    public Motion Keyframes(params (double At, Func<Motion, Motion> Set)[] frames)
    {
        if (frames is null || frames.Length == 0)
            return this;

        var grouped = new Dictionary<(BindableProperty? Property, SemanticTrack Semantic), List<MotionKeyframe>>();
        var lastOffset = new Dictionary<(BindableProperty? Property, SemanticTrack Semantic), double>();
        foreach (var (at, set) in frames)
        {
            ArgumentNullException.ThrowIfNull(set);
            var offset = double.IsNaN(at) || double.IsInfinity(at) ? 0 : Math.Clamp(at, 0, 1);
            var snapshot = set(None);
            foreach (var track in snapshot._tracks)
            {
                var key = (track.Property, track.Semantic);
                if (!grouped.TryGetValue(key, out var list))
                {
                    list = [];
                    grouped[key] = list;
                }

                if (lastOffset.TryGetValue(key, out var previous) && offset < previous)
                    System.Diagnostics.Debug.Assert(false, "Keyframe offsets must be non-decreasing.");

                lastOffset[key] = offset;
                list.Add(new MotionKeyframe(offset, track.To, track.Easing));
            }
        }

        var motion = this;
        foreach (var (key, list) in grouped)
        {
            if (list.Count == 0)
                continue;
            motion = motion.Append(new MotionTrack(
                key.Property,
                key.Semantic,
                null,
                list[^1].Value,
                0,
                1,
                null,
                list));
        }

        return motion;
    }

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
    /// Delay each bound target on linear player time. Root only; ignored on
    /// nested children. Example: 300 ms motion, two views, stagger 100 ms →
    /// player span 400 ms. View 0 window [0, 300); view 1 [100, 400). At t=0
    /// both are already at from. During [0, 100) the second target holds from.
    /// </summary>
    public Motion Stagger(
        uint stepMilliseconds,
        StaggerFrom from = StaggerFrom.Start,
        (int Columns, int Rows)? grid = null)
        => new(Duration, Easing, _tracks, new Stagger(stepMilliseconds, from, grid));

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
        return new Motion(span, easing, tracks, right._stagger ?? left._stagger);
    }

    public MotionPlayer Bind(params VisualElement[] targets)
        => MotionPlayer.Create(this, targets);

    public MotionPlayer Bind(IEnumerable<VisualElement> targets)
        => MotionPlayer.Create(this, targets as IReadOnlyList<VisualElement> ?? [.. targets]);

    Motion Add(BindableProperty property, object? from, object to)
        => Append(new MotionTrack(property, SemanticTrack.None, from, to, 0, 1, null, null));

    Motion AddSemantic(SemanticTrack semantic, object? from, object to)
        => Append(new MotionTrack(null, semantic, from, to, 0, 1, null, null));

    Motion AddKeyframes<T>(
        BindableProperty? property,
        SemanticTrack semantic,
        Func<KeyframeBuilder<T>, KeyframeBuilder<T>> frames)
    {
        ArgumentNullException.ThrowIfNull(frames);
        var list = frames(new KeyframeBuilder<T>()).Build();
        if (list.Count == 0)
            return this;
        return Append(new MotionTrack(property, semantic, null, list[^1].Value, 0, 1, null, list));
    }

    Motion AddKeyframesBoth(
        BindableProperty first,
        BindableProperty second,
        Func<KeyframeBuilder<double>, KeyframeBuilder<double>> frames)
    {
        ArgumentNullException.ThrowIfNull(frames);
        var list = frames(new KeyframeBuilder<double>()).Build();
        if (list.Count == 0)
            return this;
        var to = list[^1].Value;
        return Append(new MotionTrack(first, SemanticTrack.None, null, to, 0, 1, null, list))
            .Append(new MotionTrack(second, SemanticTrack.None, null, to, 0, 1, null, list));
    }

    Motion Append(MotionTrack track)
    {
        var tracks = new MotionTrack[_tracks.Count + 1];
        for (var i = 0; i < _tracks.Count; i++)
            tracks[i] = _tracks[i];
        tracks[_tracks.Count] = track;
        return new Motion(Duration, Easing, tracks, _stagger);
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
