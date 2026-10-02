using Microsoft.Maui.Controls.Shapes;

namespace Reactor.Animate;

/// <summary>Which edge the view travels from or toward.</summary>
public enum SlideFrom
{
    /// <summary>Negative <see cref="VisualElement.TranslationX"/>.</summary>
    Left,

    /// <summary>Positive <see cref="VisualElement.TranslationX"/>.</summary>
    Right,

    /// <summary>Negative <see cref="VisualElement.TranslationY"/>.</summary>
    Top,

    /// <summary>Positive <see cref="VisualElement.TranslationY"/>.</summary>
    Bottom,
}

/// <summary>
/// Immutable in-page motion recipe. Holds no targets. Bind at play time.
/// </summary>
public sealed class Motion
{
    /// <summary>
    /// Empty recipe. 300 ms, <see cref="Easing.CubicOut"/>, one play, HSV colors.
    /// </summary>
    public static Motion None { get; } = new(
        Timing.MotionDuration,
        Timing.MotionEasing,
        [], null, 1, false,
        [], ColorSpace.Hsv, null, null);

    readonly IReadOnlyList<MotionTrack> _tracks;
    readonly Stagger? _stagger;
    readonly int _repeat;
    readonly bool _yoyo;
    readonly IReadOnlyList<NamedSpan> _spans;
    readonly ColorSpace _colorSpace;
    readonly Spring? _spring;
    readonly double? _perspective;

    Motion(
        uint duration,
        Easing easing,
        IReadOnlyList<MotionTrack> tracks,
        Stagger? stagger,
        int repeat,
        bool yoyo,
        IReadOnlyList<NamedSpan> spans,
        ColorSpace colorSpace,
        Spring? spring,
        double? perspective)
    {
        Duration = duration;
        Easing = easing;
        _tracks = tracks;
        _stagger = stagger;
        _repeat = repeat;
        _yoyo = yoyo;
        _spans = spans;
        _colorSpace = colorSpace;
        _spring = spring;
        _perspective = perspective;
    }

    /// <summary>
    /// Recipe length in milliseconds. Default is 300. This is not the player
    /// length after <see cref="Stagger"/>.
    /// </summary>
    public uint Duration { get; }

    /// <summary>
    /// Curve for tracks that do not set their own. Default is <see cref="Easing.CubicOut"/>.
    /// </summary>
    public Easing Easing { get; }

    internal Stagger? StaggerSpec => _stagger;

    internal int RepeatCount => _repeat;

    internal bool YoyoEnabled => _yoyo;

    internal IReadOnlyList<MotionTrack> Tracks => _tracks;

    internal IReadOnlyList<NamedSpan> NamedSpans => _spans;

    internal ColorSpace ColorSpace => _colorSpace;

    internal Spring? Spring => _spring;

    /// <summary>
    /// Flutter <c>Matrix4.setEntry(3, 2, entry)</c>. Null leaves the platform
    /// eye distance. <c>0</c> is orthographic.
    /// </summary>
    internal double? PerspectiveEntry => _perspective;

    /// <summary>
    /// Sets the length in milliseconds. On a full-span recipe, replaces
    /// <see cref="Duration"/>. On a windowed recipe, a shorter value is ignored
    /// and a longer value pads the end.
    /// A debug assert fires when <see cref="WithSpring"/> is already set.
    /// </summary>
    public Motion WithDuration(uint milliseconds)
    {
        if (_spring is not null)
            System.Diagnostics.Debug.Assert(false, "WithDuration is ignored when WithSpring is set.");

        if (milliseconds == Duration)
            return this;

        var fullSpan = _tracks.Count == 0 || AllFullSpan(_tracks);
        if (fullSpan)
            return Copy(duration: milliseconds);

        var span = Math.Max(Duration, milliseconds);
        if (span == Duration)
            return this;
        return Copy(
            duration: span,
            tracks: Rebase(_tracks, Duration, span),
            spans: RebaseSpans(_spans, Duration, span));
    }

    /// <summary>
    /// Replaces the easing. <see langword="null"/> throws.
    /// Default is <see cref="Easing.CubicOut"/>.
    /// </summary>
    public Motion WithEasing(Easing easing)
        => Copy(easing: easing ?? throw new ArgumentNullException(nameof(easing)));

    /// <summary>
    /// Play with a mass-spring-damper until rest. Mutually exclusive with
    /// <see cref="WithDuration"/> / <see cref="Stagger"/>. Not an easing curve.
    /// </summary>
    public Motion WithSpring(Spring spring)
    {
        if (_stagger is not null)
            System.Diagnostics.Debug.Assert(false, "Stagger is ignored when WithSpring is set.");
        return Copy(spring: spring, setSpring: true);
    }

    /// <summary>
    /// Color interpolation. Default is <see cref="ColorSpace.Hsv"/> (shortest
    /// hue). Use <see cref="ColorSpace.Rgb"/> for channel-wise lerp.
    /// </summary>
    public Motion WithColorSpace(ColorSpace colorSpace)
        => Copy(colorSpace: colorSpace);

    /// <summary>
    /// Fades opacity to <paramref name="to"/>. The live value is captured on the first forward.
    /// </summary>
    public Motion Opacity(double to)
        => Add(VisualElement.OpacityProperty, null, to);

    /// <summary>
    /// Fades opacity from <paramref name="from"/> to <paramref name="to"/>.
    /// The first forward writes <paramref name="from"/>.
    /// </summary>
    public Motion Opacity(double from, double to)
        => Add(VisualElement.OpacityProperty, from, to);

    /// <summary>Opacity stops at 0–1 of this motion.</summary>
    public Motion Opacity(Func<KeyframeBuilder<double>, KeyframeBuilder<double>> frames)
        => AddKeyframes(VisualElement.OpacityProperty, SemanticTrack.None, frames);

    /// <summary>
    /// View translation in device-independent pixels (to-only).
    /// Not <see cref="Hero.Translate"/> (FLIP invert extra).
    /// </summary>
    public Motion Translate(double x, double y)
        => TranslateX(x).TranslateY(y);

    /// <summary>
    /// View translation in device-independent pixels.
    /// Not <see cref="Hero.Translate"/> (FLIP invert extra).
    /// </summary>
    public Motion Translate(double fromX, double fromY, double toX, double toY)
        => TranslateX(fromX, toX).TranslateY(fromY, toY);

    /// <summary>
    /// Horizontal translation, in device-independent pixels, to <paramref name="to"/>.
    /// The live value is captured on the first forward.
    /// </summary>
    public Motion TranslateX(double to)
        => Add(VisualElement.TranslationXProperty, null, to);

    /// <summary>
    /// Horizontal translation, in device-independent pixels, from <paramref name="from"/>
    /// to <paramref name="to"/>. The first forward writes <paramref name="from"/>.
    /// </summary>
    public Motion TranslateX(double from, double to)
        => Add(VisualElement.TranslationXProperty, from, to);

    /// <summary>Horizontal translation stops at 0–1 of this motion.</summary>
    public Motion TranslateX(Func<KeyframeBuilder<double>, KeyframeBuilder<double>> frames)
        => AddKeyframes(VisualElement.TranslationXProperty, SemanticTrack.None, frames);

    /// <summary>
    /// Vertical translation, in device-independent pixels, to <paramref name="to"/>.
    /// The live value is captured on the first forward.
    /// </summary>
    public Motion TranslateY(double to)
        => Add(VisualElement.TranslationYProperty, null, to);

    /// <summary>
    /// Vertical translation, in device-independent pixels, from <paramref name="from"/>
    /// to <paramref name="to"/>. The first forward writes <paramref name="from"/>.
    /// </summary>
    public Motion TranslateY(double from, double to)
        => Add(VisualElement.TranslationYProperty, from, to);

    /// <summary>Vertical translation stops at 0–1 of this motion.</summary>
    public Motion TranslateY(Func<KeyframeBuilder<double>, KeyframeBuilder<double>> frames)
        => AddKeyframes(VisualElement.TranslationYProperty, SemanticTrack.None, frames);

    /// <summary>
    /// Writes <see cref="VisualElement.TranslationXProperty"/> and
    /// <see cref="VisualElement.TranslationYProperty"/> along
    /// <paramref name="geometry"/>. <paramref name="from"/> / <paramref name="to"/>
    /// are 0–1 of path length. Stagger delays the start of path <c>t</c>.
    /// </summary>
    public Motion Path(PathGeometry geometry, double from = 0, double to = 1)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        from = double.IsNaN(from) || double.IsInfinity(from) ? 0 : Math.Clamp(from, 0, 1);
        to = double.IsNaN(to) || double.IsInfinity(to) ? 1 : Math.Clamp(to, 0, 1);
        return Append(new MotionTrack(null, SemanticTrack.Path, null, to, 0, 1, null, null)
        {
            Path = geometry,
            PathFrom = from,
            PathTo = to,
        });
    }

    /// <summary>
    /// Writes both <see cref="VisualElement.ScaleXProperty"/> and
    /// <see cref="VisualElement.ScaleYProperty"/> (not <c>ScaleProperty</c>).
    /// </summary>
    public Motion Scale(double to)
        => ScaleX(to).ScaleY(to);

    /// <summary>
    /// Writes both <see cref="VisualElement.ScaleXProperty"/> and
    /// <see cref="VisualElement.ScaleYProperty"/> from <paramref name="from"/> to <paramref name="to"/>.
    /// The first forward writes <paramref name="from"/>.
    /// </summary>
    public Motion Scale(double from, double to)
        => ScaleX(from, to).ScaleY(from, to);

    /// <summary>
    /// Keyframe stops for both <see cref="VisualElement.ScaleXProperty"/> and
    /// <see cref="VisualElement.ScaleYProperty"/>. Offsets are 0–1 of this motion.
    /// </summary>
    public Motion Scale(Func<KeyframeBuilder<double>, KeyframeBuilder<double>> frames)
        => AddKeyframesBoth(VisualElement.ScaleXProperty, VisualElement.ScaleYProperty, frames);

    /// <summary>
    /// Horizontal scale to <paramref name="to"/>. The live value is captured on the first forward.
    /// </summary>
    public Motion ScaleX(double to)
        => Add(VisualElement.ScaleXProperty, null, to);

    /// <summary>
    /// Horizontal scale from <paramref name="from"/> to <paramref name="to"/>.
    /// The first forward writes <paramref name="from"/>.
    /// </summary>
    public Motion ScaleX(double from, double to)
        => Add(VisualElement.ScaleXProperty, from, to);

    /// <summary>Horizontal scale stops at 0–1 of this motion.</summary>
    public Motion ScaleX(Func<KeyframeBuilder<double>, KeyframeBuilder<double>> frames)
        => AddKeyframes(VisualElement.ScaleXProperty, SemanticTrack.None, frames);

    /// <summary>
    /// Vertical scale to <paramref name="to"/>. The live value is captured on the first forward.
    /// </summary>
    public Motion ScaleY(double to)
        => Add(VisualElement.ScaleYProperty, null, to);

    /// <summary>
    /// Vertical scale from <paramref name="from"/> to <paramref name="to"/>.
    /// The first forward writes <paramref name="from"/>.
    /// </summary>
    public Motion ScaleY(double from, double to)
        => Add(VisualElement.ScaleYProperty, from, to);

    /// <summary>Vertical scale stops at 0–1 of this motion.</summary>
    public Motion ScaleY(Func<KeyframeBuilder<double>, KeyframeBuilder<double>> frames)
        => AddKeyframes(VisualElement.ScaleYProperty, SemanticTrack.None, frames);

    /// <summary>
    /// View rotation in degrees. Not <see cref="Hero.Rotate"/> (FLIP invert extra).
    /// </summary>
    public Motion Rotate(double toDegrees)
        => Add(VisualElement.RotationProperty, null, toDegrees);

    /// <summary>
    /// In-plane rotation, in degrees, from <paramref name="fromDegrees"/> to <paramref name="toDegrees"/>.
    /// The first forward writes <paramref name="fromDegrees"/>.
    /// </summary>
    public Motion Rotate(double fromDegrees, double toDegrees)
        => Add(VisualElement.RotationProperty, fromDegrees, toDegrees);

    /// <summary>In-plane rotation stops, in degrees, at 0–1 of this motion.</summary>
    public Motion Rotate(Func<KeyframeBuilder<double>, KeyframeBuilder<double>> frames)
        => AddKeyframes(VisualElement.RotationProperty, SemanticTrack.None, frames);

    /// <summary>
    /// Rotation about the horizontal axis, in degrees, around
    /// <see cref="VisualElement.AnchorX"/> and <see cref="VisualElement.AnchorY"/>.
    /// Pair with <see cref="Perspective(double)"/> so the far edge shrinks.
    /// </summary>
    public Motion RotateX(double toDegrees)
        => Add(VisualElement.RotationXProperty, null, toDegrees);

    /// <summary>
    /// Rotation about the horizontal axis, in degrees, from <paramref name="fromDegrees"/>
    /// to <paramref name="toDegrees"/>. The first forward writes <paramref name="fromDegrees"/>.
    /// </summary>
    public Motion RotateX(double fromDegrees, double toDegrees)
        => Add(VisualElement.RotationXProperty, fromDegrees, toDegrees);

    /// <summary>Horizontal-axis rotation stops, in degrees, at 0–1 of this motion.</summary>
    public Motion RotateX(Func<KeyframeBuilder<double>, KeyframeBuilder<double>> frames)
        => AddKeyframes(VisualElement.RotationXProperty, SemanticTrack.None, frames);

    /// <summary>
    /// Rotation about the vertical axis, in degrees, around
    /// <see cref="VisualElement.AnchorX"/> and <see cref="VisualElement.AnchorY"/>.
    /// Pair with <see cref="Perspective(double)"/> so the far edge shrinks.
    /// </summary>
    public Motion RotateY(double toDegrees)
        => Add(VisualElement.RotationYProperty, null, toDegrees);

    /// <summary>
    /// Rotation about the vertical axis, in degrees, from <paramref name="fromDegrees"/>
    /// to <paramref name="toDegrees"/>. The first forward writes <paramref name="fromDegrees"/>.
    /// </summary>
    public Motion RotateY(double fromDegrees, double toDegrees)
        => Add(VisualElement.RotationYProperty, fromDegrees, toDegrees);

    /// <summary>Vertical-axis rotation stops, in degrees, at 0–1 of this motion.</summary>
    public Motion RotateY(Func<KeyframeBuilder<double>, KeyframeBuilder<double>> frames)
        => AddKeyframes(VisualElement.RotationYProperty, SemanticTrack.None, frames);

    /// <summary>
    /// Eye distance for <see cref="RotateX(double)"/> and <see cref="RotateY(double)"/>.
    /// <paramref name="entry"/> is Flutter's <c>Matrix4.setEntry(3, 2, entry)</c>:
    /// the camera sits <c>1/entry</c> device-independent pixels away.
    /// <c>0.001</c> shrinks the far edge a little. <c>0</c> keeps both edges the same height.
    /// Windows keeps its plane projection and does not use this distance.
    /// </summary>
    public Motion Perspective(double entry)
    {
        if (double.IsNaN(entry) || double.IsInfinity(entry))
            entry = 0;
        return Copy(perspective: entry, setPerspective: true);
    }

    /// <summary>
    /// Blends the background to <paramref name="to"/> in the recipe
    /// <see cref="Reactor.Animate.ColorSpace"/>. The live color is captured on the first forward.
    /// </summary>
    public Motion BackgroundColor(Color to)
        => Add(VisualElement.BackgroundColorProperty, null, to);

    /// <summary>
    /// Blends the background from <paramref name="from"/> to <paramref name="to"/>
    /// in the recipe <see cref="Reactor.Animate.ColorSpace"/>. The first forward writes <paramref name="from"/>.
    /// </summary>
    public Motion BackgroundColor(Color from, Color to)
        => Add(VisualElement.BackgroundColorProperty, from, to);

    /// <summary>Background-color stops at 0–1 of this motion.</summary>
    public Motion BackgroundColor(Func<KeyframeBuilder<Color>, KeyframeBuilder<Color>> frames)
        => AddKeyframes(VisualElement.BackgroundColorProperty, SemanticTrack.None, frames);

    /// <summary>
    /// Animates <see cref="VisualElement.WidthRequest"/> to <paramref name="to"/>,
    /// in device-independent pixels. The live value is captured on the first forward.
    /// </summary>
    public Motion Width(double to)
        => Add(VisualElement.WidthRequestProperty, null, to);

    /// <summary>
    /// Animates <see cref="VisualElement.WidthRequest"/> from <paramref name="from"/>
    /// to <paramref name="to"/>. The first forward writes <paramref name="from"/>.
    /// </summary>
    public Motion Width(double from, double to)
        => Add(VisualElement.WidthRequestProperty, from, to);

    /// <summary>Width stops at 0–1 of this motion.</summary>
    public Motion Width(Func<KeyframeBuilder<double>, KeyframeBuilder<double>> frames)
        => AddKeyframes(VisualElement.WidthRequestProperty, SemanticTrack.None, frames);

    /// <summary>
    /// Animates <see cref="VisualElement.HeightRequest"/> to <paramref name="to"/>,
    /// in device-independent pixels. The live value is captured on the first forward.
    /// </summary>
    public Motion Height(double to)
        => Add(VisualElement.HeightRequestProperty, null, to);

    /// <summary>
    /// Animates <see cref="VisualElement.HeightRequest"/> from <paramref name="from"/>
    /// to <paramref name="to"/>. The first forward writes <paramref name="from"/>.
    /// </summary>
    public Motion Height(double from, double to)
        => Add(VisualElement.HeightRequestProperty, from, to);

    /// <summary>Height stops at 0–1 of this motion.</summary>
    public Motion Height(Func<KeyframeBuilder<double>, KeyframeBuilder<double>> frames)
        => AddKeyframes(VisualElement.HeightRequestProperty, SemanticTrack.None, frames);

    /// <summary>
    /// Animates the corner radius to <paramref name="to"/>.
    /// The live value is captured on the first forward.
    /// </summary>
    public Motion CornerRadius(CornerRadius to)
        => AddSemantic(SemanticTrack.CornerRadius, null, to);

    /// <summary>
    /// Animates the corner radius from <paramref name="from"/> to <paramref name="to"/>.
    /// The first forward writes <paramref name="from"/>.
    /// </summary>
    public Motion CornerRadius(CornerRadius from, CornerRadius to)
        => AddSemantic(SemanticTrack.CornerRadius, from, to);

    /// <summary>Corner-radius stops at 0–1 of this motion.</summary>
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

    /// <summary>
    /// Animates <paramref name="property"/> to <paramref name="to"/>.
    /// The live value is captured on the first forward.
    /// </summary>
    public Motion Property(BindableProperty property, object to)
        => Add(property ?? throw new ArgumentNullException(nameof(property)), null, to);

    /// <summary>
    /// Animates <paramref name="property"/> from <paramref name="from"/> to <paramref name="to"/>.
    /// The first forward writes <paramref name="from"/>.
    /// </summary>
    public Motion Property(BindableProperty property, object from, object to)
        => Add(property ?? throw new ArgumentNullException(nameof(property)), from, to);

    /// <summary>Opacity from 0 to 1.</summary>
    public Motion FadeIn()
        => Opacity(0, 1);

    /// <summary>Opacity from 1 to 0.</summary>
    public Motion FadeOut()
        => Opacity(1, 0);

    /// <summary>
    /// Slides in from <paramref name="from"/> by <paramref name="distance"/>
    /// device-independent pixels. Default distance is 24.
    /// The first forward writes the offset.
    /// </summary>
    public Motion SlideIn(SlideFrom from, double distance = 24)
        => from switch
        {
            SlideFrom.Left => TranslateX(-distance, 0),
            SlideFrom.Right => TranslateX(distance, 0),
            SlideFrom.Top => TranslateY(-distance, 0),
            SlideFrom.Bottom => TranslateY(distance, 0),
            _ => TranslateX(-distance, 0),
        };

    /// <summary>
    /// Slides out toward <paramref name="to"/> by <paramref name="distance"/>
    /// device-independent pixels. Default distance is 24.
    /// </summary>
    public Motion SlideOut(SlideFrom to, double distance = 24)
        => to switch
        {
            SlideFrom.Left => TranslateX(0, -distance),
            SlideFrom.Right => TranslateX(0, distance),
            SlideFrom.Top => TranslateY(0, -distance),
            SlideFrom.Bottom => TranslateY(0, distance),
            _ => TranslateX(0, -distance),
        };

    /// <summary>
    /// Scales both axes from <paramref name="from"/> to 1. Default start is 0.85.
    /// The first forward writes <paramref name="from"/>.
    /// </summary>
    public Motion ScaleIn(double from = 0.85)
        => Scale(from, 1);

    /// <summary>Scales both axes from 1 to <paramref name="to"/>. Default end is 0.85.</summary>
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
    {
        if (_spring is not null)
            System.Diagnostics.Debug.Assert(false, "Stagger is ignored when WithSpring is set.");
        return Copy(stagger: new Stagger(stepMilliseconds, from, grid));
    }

    /// <summary>
    /// Play this motion <paramref name="count"/> times. <c>1</c> is once
    /// (default). <c>0</c> becomes <c>1</c>. <c>-1</c> repeats until Pause, Reset, or Dispose.
    /// </summary>
    public Motion Repeat(int count)
        => Copy(repeat: count == 0 ? 1 : count);

    /// <summary>
    /// After each forward, play reverse. A repeat cycle is forward+reverse.
    /// </summary>
    public Motion Yoyo(bool enabled = true)
        => Copy(yoyo: enabled);

    /// <summary>
    /// Places <paramref name="child"/> on this timeline at
    /// <paramref name="at"/> milliseconds. Nested <see cref="Stagger"/> is
    /// ignored. Parent span is <c>max(current, at + child.Duration)</c>.
    /// Duplicate <paramref name="id"/> values: last wins.
    /// </summary>
    public Motion Add(Motion child, uint at = 0, string? id = null)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (child._stagger is not null)
            System.Diagnostics.Debug.WriteLine("Motion.Add: nested Stagger is ignored (root only).");

        var childSpan = child.Duration;
        var newSpan = Math.Max(Duration, at + childSpan);
        var existing = Duration == newSpan ? _tracks : Rebase(_tracks, Duration, newSpan);
        var shifted = Shift(child._tracks, at, childSpan, newSpan);
        WarnOverlap(existing, shifted);

        var spans = ConcatSpans(
            Duration == newSpan ? _spans : RebaseSpans(_spans, Duration, newSpan),
            ShiftSpans(child._spans, at, childSpan, newSpan));
        if (!string.IsNullOrWhiteSpace(id) && newSpan > 0)
        {
            spans = PutSpan(spans, new NamedSpan(
                id,
                (double)at / newSpan,
                (double)(at + childSpan) / newSpan));
        }

        return Copy(
            duration: newSpan,
            tracks: Concat(existing, shifted),
            spans: spans,
            perspective: child._perspective,
            setPerspective: child._perspective.HasValue);
    }

    /// <summary>
    /// Appends <paramref name="next"/> at the current span (after every track
    /// already on this motion). Use <see cref="Add(Motion, uint, string?)"/> to start at 0.
    /// </summary>
    public Motion Then(Motion next, string? id = null)
        => Add(next, Duration, id);

    /// <summary>
    /// Plays <paramref name="other"/> at the same time. The parent span is the
    /// longer recipe. Tracks keep their length in milliseconds and are not stretched.
    /// Where tracks overlap, <paramref name="other"/> wins.
    /// </summary>
    public Motion And(Motion other)
    {
        ArgumentNullException.ThrowIfNull(other);

        var span = Math.Max(Duration, other.Duration);
        var easing = !ReferenceEquals(other.Easing, Timing.MotionEasing)
            ? other.Easing
            : Easing;
        var tracks = Concat(Rebase(_tracks, Duration, span), Rebase(other._tracks, other.Duration, span));
        var spans = ConcatSpans(
            RebaseSpans(_spans, Duration, span),
            RebaseSpans(other._spans, other.Duration, span));
        return new Motion(
            span,
            easing,
            tracks,
            other._stagger ?? _stagger,
            other._repeat != 1 ? other._repeat : _repeat,
            other._yoyo || _yoyo,
            spans,
            other._colorSpace,
            other._spring ?? _spring,
            other._perspective ?? _perspective);
    }

    /// <summary>
    /// Binds this recipe to <paramref name="targets"/>. Does not start playback.
    /// </summary>
    public MotionPlayer Bind(params VisualElement[] targets)
        => MotionPlayer.Create(this, targets);

    /// <summary>
    /// Binds this recipe to <paramref name="targets"/>. Does not start playback.
    /// </summary>
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
        return Copy(tracks: tracks);
    }

    Motion Copy(
        uint? duration = null,
        Easing? easing = null,
        IReadOnlyList<MotionTrack>? tracks = null,
        Stagger? stagger = null,
        int? repeat = null,
        bool? yoyo = null,
        IReadOnlyList<NamedSpan>? spans = null,
        ColorSpace? colorSpace = null,
        Spring? spring = null,
        bool setSpring = false,
        double? perspective = null,
        bool setPerspective = false)
        => new(
            duration ?? Duration,
            easing ?? Easing,
            tracks ?? _tracks,
            stagger ?? _stagger,
            repeat ?? _repeat,
            yoyo ?? _yoyo,
            spans ?? _spans,
            colorSpace ?? _colorSpace,
            setSpring ? spring : _spring,
            setPerspective ? perspective : _perspective);

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

    static IReadOnlyList<MotionTrack> Shift(
        IReadOnlyList<MotionTrack> tracks,
        uint atMs,
        uint childSpan,
        uint parentSpan)
    {
        if (tracks.Count == 0 || parentSpan == 0)
            return tracks;

        var shifted = new MotionTrack[tracks.Count];
        for (var i = 0; i < tracks.Count; i++)
        {
            var track = tracks[i];
            shifted[i] = track with
            {
                Begin = (atMs + track.Begin * childSpan) / parentSpan,
                End = (atMs + track.End * childSpan) / parentSpan,
            };
        }

        return shifted;
    }

    static void WarnOverlap(IReadOnlyList<MotionTrack> existing, IReadOnlyList<MotionTrack> incoming)
    {
        for (var i = 0; i < incoming.Count; i++)
        {
            var next = incoming[i];
            for (var j = 0; j < existing.Count; j++)
            {
                var prior = existing[j];
                if (prior.Property != next.Property || prior.Semantic != next.Semantic)
                    continue;
                if (prior.Begin < next.End && next.Begin < prior.End)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Motion: overlapping {next.Property?.PropertyName ?? next.Semantic.ToString()} tracks; later track wins.");
                }
            }
        }
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

    static IReadOnlyList<NamedSpan> RebaseSpans(IReadOnlyList<NamedSpan> spans, uint fromSpan, uint toSpan)
    {
        if (fromSpan == toSpan || toSpan == 0 || spans.Count == 0)
            return spans;

        var scale = (double)fromSpan / toSpan;
        var rebased = new NamedSpan[spans.Count];
        for (var i = 0; i < spans.Count; i++)
        {
            var span = spans[i];
            rebased[i] = span with { Begin = span.Begin * scale, End = span.End * scale };
        }

        return rebased;
    }

    static IReadOnlyList<NamedSpan> ShiftSpans(
        IReadOnlyList<NamedSpan> spans,
        uint atMs,
        uint childSpan,
        uint parentSpan)
    {
        if (spans.Count == 0 || parentSpan == 0)
            return spans;

        var shifted = new NamedSpan[spans.Count];
        for (var i = 0; i < spans.Count; i++)
        {
            var span = spans[i];
            shifted[i] = span with
            {
                Begin = (atMs + span.Begin * childSpan) / parentSpan,
                End = (atMs + span.End * childSpan) / parentSpan,
            };
        }

        return shifted;
    }

    static IReadOnlyList<NamedSpan> ConcatSpans(
        IReadOnlyList<NamedSpan> left,
        IReadOnlyList<NamedSpan> right)
    {
        if (left.Count == 0)
            return right;
        if (right.Count == 0)
            return left;

        var spans = new NamedSpan[left.Count + right.Count];
        for (var i = 0; i < left.Count; i++)
            spans[i] = left[i];
        for (var i = 0; i < right.Count; i++)
            spans[left.Count + i] = right[i];
        return CompactSpans(spans);
    }

    static IReadOnlyList<NamedSpan> PutSpan(IReadOnlyList<NamedSpan> spans, NamedSpan next)
        => CompactSpans(ConcatSpans(spans, [next]));

    static IReadOnlyList<NamedSpan> CompactSpans(IReadOnlyList<NamedSpan> spans)
    {
        if (spans.Count <= 1)
            return spans;

        var last = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < spans.Count; i++)
            last[spans[i].Id] = i;

        if (last.Count == spans.Count)
            return spans;

        System.Diagnostics.Debug.WriteLine("Motion: duplicate timeline id; last wins.");

        var compact = new NamedSpan[last.Count];
        var w = 0;
        for (var i = 0; i < spans.Count; i++)
        {
            if (last[spans[i].Id] == i)
                compact[w++] = spans[i];
        }

        return compact;
    }
}
