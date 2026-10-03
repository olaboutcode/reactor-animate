using Reactor.Animate;

namespace Reactor.Animate.Internals;

/// <summary>
/// Applies one <see cref="MotionTrack"/> to one view on each tick. Skips a view
/// <see cref="FlightPins"/> is holding so a motion does not fight a hero clip.
/// A missing <c>From</c> is captured on the first forward and written back on reset.
/// The player already roots the view, so the runtime keeps that reference.
/// A tick that produces the same value does not write again, so a stagger hold
/// does not invalidate the view every frame. Skipping a pinned view forgets the
/// last value. The next free tick writes, even when the motion value matches.
/// </summary>
internal sealed class TrackRuntime
{
    readonly VisualElement _target;
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
    readonly Func<double, Matrix4>? _transform;
    readonly bool _implicitFrom;
    object? _from;
    object? _lastWritten;
    bool _loggedPinSkip;
    bool _colorPrepared;
    CachedColorLerp _colorLerp;
    HsvColor[]? _keyframeHsv;
    bool _hasPoint;
    Point _lastPoint;

    public TrackRuntime(
        VisualElement target,
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
        double? perspective = null,
        Func<double, Matrix4>? transform = null)
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
        _transform = transform;
        _implicitFrom = from is null;
    }

    public void CaptureAndWriteFrom()
    {
        ForgetLastWrite();
        if (!TryTarget(out var view))
            return;

        _loggedPinSkip = false;
        if (_transform is not null)
        {
            if (!SkipPinned(view))
                MatrixPlane.Apply(view, _transform(0) ?? Matrix4.Identity);
            return;
        }

        if (_path is not null)
        {
            if (!SkipPinned(view))
                WritePoint(view, _path.PointAt(_pathFrom));
            return;
        }

        if (_implicitFrom)
            _from = view.GetValue(_property);

        if (_from is not null && !SkipPinned(view))
            Commit(view, _from);
    }

    public void WriteFrom()
    {
        if (!TryTarget(out var view) || SkipPinned(view))
            return;
        if (_transform is not null)
        {
            MatrixPlane.Apply(view, _transform(0) ?? Matrix4.Identity);
            return;
        }

        if (_path is not null)
        {
            WritePoint(view, _path.PointAt(_pathFrom));
            return;
        }

        if (_from is null)
            return;
        Commit(view, _from);
    }

    public void Apply(double u)
    {
        if (!TryTarget(out var view) || SkipPinned(view))
            return;

        if (_transform is not null)
        {
            MatrixPlane.Apply(view, _transform(Eased(u)) ?? Matrix4.Identity);
            return;
        }

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

        PrepareColors();
        var local = LocalU(u);
        object value;
        if (_keyframes is not null)
        {
            value = EvaluateKeyframes(local, _from);
        }
        else if (u <= _begin || _end <= _begin)
        {
            value = _from;
        }
        else if (u >= _end)
        {
            value = _to;
        }
        else
        {
            var eased = _easing.Ease(local);
            value = _colorLerp.LerpOrNull(eased)
                ?? PropertyLerp.Lerp(_from, _to, eased, _colorSpace)
                ?? _to;
        }

        Commit(view, value);
    }

    void Commit(VisualElement view, object value)
    {
        if (Equals(_lastWritten, value))
        {
            // Perspective is not a bindable property. Keep applying it so a
            // handler that attaches during a hold still receives the camera.
            if (_perspective is not null)
                ApplyPerspective(view);
            return;
        }

        _lastWritten = value;
        PropertyLerp.Write(view, _property, value);
        ApplyPerspective(view);
    }

    void PrepareColors()
    {
        if (_colorPrepared)
            return;

        _colorPrepared = true;
        _colorLerp.Prepare(_from, _to, _colorSpace);
        if (_keyframes is not { Count: > 0 } frames || _colorSpace == ColorSpace.Rgb)
            return;

        var hsv = new HsvColor[frames.Count];
        for (var i = 0; i < frames.Count; i++)
        {
            if (frames[i].Value is not Color color)
                return;

            hsv[i] = HsvColor.From(color);
        }

        _keyframeHsv = hsv;
    }

    void ApplyPerspective(VisualElement view)
    {
        if (_perspective is not double entry)
            return;
        if (_property != VisualElement.RotationProperty
            && _property != VisualElement.RotationXProperty
            && _property != VisualElement.RotationYProperty)
        {
            return;
        }

        PerspectivePlane.Apply(view, entry);
    }

    double Eased(double u)
    {
        if (u <= _begin || _end <= _begin)
            return 0;
        if (u >= _end)
            return 1;
        return _easing.Ease(LocalU(u));
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

    object EvaluateKeyframes(double local, object from)
    {
        var frames = _keyframes!;
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
            var curve = next.Easing ?? _easing;
            if (_keyframeHsv is not null)
                return _keyframeHsv[i].Lerp(_keyframeHsv[i + 1], curve.Ease(s));

            return PropertyLerp.Lerp(current.Value, next.Value, curve.Ease(s), _colorSpace) ?? next.Value;
        }

        return frames[^1].Value;
    }

    void WritePoint(VisualElement view, Point point)
    {
        if (_hasPoint && _lastPoint.X == point.X && _lastPoint.Y == point.Y)
            return;

        _hasPoint = true;
        _lastPoint = point;
        PropertyLerp.Write(view, VisualElement.TranslationXProperty, point.X);
        PropertyLerp.Write(view, VisualElement.TranslationYProperty, point.Y);
    }

    void ForgetLastWrite()
    {
        _colorPrepared = false;
        _keyframeHsv = null;
        _lastWritten = null;
        _hasPoint = false;
    }

    bool SkipPinned(VisualElement view)
    {
        if (!FlightPins.IsPinned(view))
            return false;

        // The clip may have changed the property. The next free tick must write.
        _lastWritten = null;
        _hasPoint = false;
        if (!_loggedPinSkip)
        {
            _loggedPinSkip = true;
            System.Diagnostics.Debug.WriteLine("Motion: skip writes on pinned hero.");
        }

        return true;
    }

    bool TryTarget(out VisualElement view)
    {
        view = _target;
        if (view.Handler is not null && !view.IsLoaded)
        {
            _lastWritten = null;
            _hasPoint = false;
            return false;
        }

        return true;
    }
}
