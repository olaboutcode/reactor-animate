namespace Reactor.Animate.Motion;

internal sealed class TrackRuntime
{
    readonly WeakReference<VisualElement> _target;
    readonly BindableProperty _property;
    readonly object _to;
    readonly double _begin;
    readonly double _end;
    readonly Easing _easing;
    readonly bool _implicitFrom;
    object? _from;

    public TrackRuntime(
        WeakReference<VisualElement> target,
        BindableProperty property,
        object? from,
        object to,
        double begin,
        double end,
        Easing easing)
    {
        _target = target;
        _property = property;
        _from = from;
        _to = to;
        _begin = begin;
        _end = end;
        _easing = easing;
        _implicitFrom = from is null;
    }

    public void CaptureAndWriteFrom()
    {
        if (!TryTarget(out var view))
            return;

        if (_implicitFrom)
            _from = view.GetValue(_property);

        if (_from is not null)
            PropertyLerp.Write(view, _property, _from);
    }

    public void WriteFrom()
    {
        if (_from is null || !TryTarget(out var view))
            return;
        PropertyLerp.Write(view, _property, _from);
    }

    public void Apply(double u)
    {
        if (!TryTarget(out var view))
            return;

        _from ??= view.GetValue(_property);
        if (_from is null)
            return;

        object value;
        if (u <= _begin || _end <= _begin)
            value = _from;
        else if (u >= _end)
            value = _to;
        else
        {
            var local = (u - _begin) / (_end - _begin);
            value = PropertyLerp.Lerp(_from, _to, _easing.Ease(local)) ?? _to;
        }

        PropertyLerp.Write(view, _property, value);
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
