namespace Reactor.Animate.Motion;

internal sealed class TrackRuntime
{
    readonly WeakReference<VisualElement> _target;
    readonly BindableProperty _property;
    readonly object _to;
    readonly double _begin;
    readonly double _end;
    readonly Easing _easing;
    readonly IReadOnlyList<MotionKeyframe>? _keyframes;
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
        IReadOnlyList<MotionKeyframe>? keyframes = null)
    {
        _target = target;
        _property = property;
        _from = from;
        _to = to;
        _begin = begin;
        _end = end;
        _easing = easing;
        _keyframes = keyframes is { Count: > 0 } ? keyframes : null;
        _implicitFrom = from is null;
    }

    public void CaptureAndWriteFrom()
    {
        if (!TryTarget(out var view))
            return;

        _loggedPinSkip = false;
        if (_implicitFrom)
            _from = view.GetValue(_property);

        if (_from is not null && !SkipPinned(view))
            PropertyLerp.Write(view, _property, _from);
    }

    public void WriteFrom()
    {
        if (_from is null || !TryTarget(out var view) || SkipPinned(view))
            return;
        PropertyLerp.Write(view, _property, _from);
    }

    public void Apply(double u)
    {
        if (!TryTarget(out var view) || SkipPinned(view))
            return;

        _from ??= view.GetValue(_property);
        if (_from is null)
            return;

        var local = LocalU(u);
        object value;
        if (_keyframes is not null)
            value = EvaluateKeyframes(local, _from, _easing, _keyframes);
        else if (u <= _begin || _end <= _begin)
            value = _from;
        else if (u >= _end)
            value = _to;
        else
            value = PropertyLerp.Lerp(_from, _to, _easing.Ease(local)) ?? _to;

        PropertyLerp.Write(view, _property, value);
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
        IReadOnlyList<MotionKeyframe> frames)
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
            return PropertyLerp.Lerp(current.Value, next.Value, curve.Ease(s)) ?? next.Value;
        }

        return frames[^1].Value;
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
