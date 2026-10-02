using Reactor.Animate;

namespace Reactor.Animate.Internals;

/// <summary>
/// Applies one <see cref="MotionTrack"/> to one view on each tick. Skips a view
/// <see cref="FlightPins"/> is holding so a motion does not fight a hero clip.
/// A missing <c>From</c> is captured on the first forward and written back on reset.
/// </summary>
internal sealed class TrackRuntime
{
    readonly WeakReference<VisualElement> _target;
    readonly BindableProperty _property;
    readonly object _to;
    readonly double _begin;
    readonly double _end;
    readonly Easing _easing;
    readonly IReadOnlyList<MotionKeyframe>? _keyframes;
    readonly ColorSpace _colorSpace;
    readonly PathSampler? _path;
    readonly double _pathFrom;
    readonly double _pathTo;
    readonly double? _perspective;
    readonly bool _implicitFrom;
    object? _from;
    bool _loggedPinSkip;

    public TrackRuntime(
        WeakReference<VisualElement> target,
        BindableProperty property,
        object? from,
        object to,
        double begin,
        double end,
        Easing easing,
        IReadOnlyList<MotionKeyframe>? keyframes = null,
        ColorSpace colorSpace = ColorSpace.Hsv,
        PathSampler? path = null,
        double pathFrom = 0,
        double pathTo = 1,
        double? perspective = null)
    {
        _target = target;
        _property = property;
        _from = from;
        _to = to;
        _begin = begin;
        _end = end;
        _easing = easing;
        _keyframes = keyframes is { Count: > 0 } ? keyframes : null;
        _colorSpace = colorSpace;
        _path = path;
        _pathFrom = pathFrom;
        _pathTo = pathTo;
        _perspective = perspective;
        _implicitFrom = from is null;
    }

    public void CaptureAndWriteFrom()
    {
        if (!TryTarget(out var view))
            return;

        _loggedPinSkip = false;
        if (_path is not null)
        {
            if (!SkipPinned(view))
                WritePoint(view, _path.PointAt(_pathFrom));
            return;
        }

        if (_implicitFrom)
            _from = view.GetValue(_property);

        if (_from is not null && !SkipPinned(view))
        {
            PropertyLerp.Write(view, _property, _from);
            ApplyPerspective(view);
        }
    }

    public void WriteFrom()
    {
        if (!TryTarget(out var view) || SkipPinned(view))
            return;
        if (_path is not null)
        {
            WritePoint(view, _path.PointAt(_pathFrom));
            return;
        }

        if (_from is null)
            return;
        PropertyLerp.Write(view, _property, _from);
        ApplyPerspective(view);
    }

    public void Apply(double u)
    {
        if (!TryTarget(out var view) || SkipPinned(view))
            return;

        if (_path is not null)
        {
            var pathLocal = LocalU(u);
            var t = _pathFrom + (_pathTo - _pathFrom) * _easing.Ease(pathLocal);
            if (u <= _begin || _end <= _begin)
                t = _pathFrom;
            else if (u >= _end)
                t = _pathTo;
            WritePoint(view, _path.PointAt(t));
            return;
        }

        _from ??= view.GetValue(_property);
        if (_from is null)
            return;

        var local = LocalU(u);
        object value;
        if (_keyframes is not null)
            value = EvaluateKeyframes(local, _from, _easing, _keyframes, _colorSpace);
        else if (u <= _begin || _end <= _begin)
            value = _from;
        else if (u >= _end)
            value = _to;
        else
            value = PropertyLerp.Lerp(_from, _to, _easing.Ease(local), _colorSpace) ?? _to;

        PropertyLerp.Write(view, _property, value);
        ApplyPerspective(view);
    }

    void ApplyPerspective(VisualElement view)
    {
        if (_perspective is not double entry)
            return;
        if (_property != VisualElement.RotationProperty
            && _property != VisualElement.RotationXProperty
            && _property != VisualElement.RotationYProperty)
            return;

        PerspectivePlane.Apply(view, entry);
    }

    double LocalU(double u)
    {
        if (_end <= _begin)
            return u >= _begin ? 1 : 0;
        if (u <= _begin)
            return 0;
        if (u >= _end)
            return 1;
        return (u - _begin) / (_end - _begin);
    }

    static object EvaluateKeyframes(
        double local,
        object from,
        Easing easing,
        IReadOnlyList<MotionKeyframe> frames,
        ColorSpace colorSpace)
    {
        if (local < frames[0].Offset)
            return from;

        for (var i = 0; i < frames.Count - 1; i++)
        {
            var next = frames[i + 1];
            if (local >= next.Offset)
                continue;

            var current = frames[i];
            var span = next.Offset - current.Offset;
            if (span <= 0)
                return next.Value;

            var s = (local - current.Offset) / span;
            var curve = next.Easing ?? easing;
            return PropertyLerp.Lerp(current.Value, next.Value, curve.Ease(s), colorSpace) ?? next.Value;
        }

        return frames[^1].Value;
    }

    static void WritePoint(VisualElement view, Point point)
    {
        PropertyLerp.Write(view, VisualElement.TranslationXProperty, point.X);
        PropertyLerp.Write(view, VisualElement.TranslationYProperty, point.Y);
    }

    bool SkipPinned(VisualElement view)
    {
        if (!FlightPins.IsPinned(view))
            return false;
        if (!_loggedPinSkip)
        {
            _loggedPinSkip = true;
            System.Diagnostics.Debug.WriteLine("Motion: skip writes on pinned hero.");
        }

        return true;
    }

    bool TryTarget(out VisualElement view)
    {
        if (!_target.TryGetTarget(out view!))
            return false;
        if (view.Handler is not null && !view.IsLoaded)
            return false;
        return true;
    }
}
