using Microsoft.Maui.Controls.Shapes;

namespace Reactor.Animate;

interface IMotionClip
{
    Task PlayAsync(CancellationToken cancellationToken = default);

    Task ReverseAsync(CancellationToken cancellationToken = default);

    void NotifyWhenSettled(Action action);
}

static class Motion
{
    public const uint DefaultDuration = 400;

    public static Easing DefaultEasing { get; } = Easing.CubicOut;

    public static MotionBuilder On(VisualElement view) => new(view);

    public static IMotionClip Parallel(params IMotionClip[] clips) => new ParallelClip(clips);
}

sealed class MotionBuilder
{
    readonly List<MotionTween> _tweens = [];
    VisualElement _view;
    uint _duration = Motion.DefaultDuration;
    Easing _easing = Motion.DefaultEasing;
    VisualElement? _owner;

    internal MotionBuilder(VisualElement view) => _view = view;

    public MotionBuilder On(VisualElement view)
    {
        _view = view;
        return this;
    }

    public MotionBuilder Owner(VisualElement owner)
    {
        _owner = owner;
        return this;
    }

    public MotionBuilder Duration(uint milliseconds)
    {
        _duration = milliseconds;
        return this;
    }

    public MotionBuilder Easing(Easing easing)
    {
        _easing = easing;
        return this;
    }

    public MotionBuilder To(BindableProperty property, object target, object? from = null)
    {
        _tweens.Add(new MotionTween(_view, property, target) { From = from });
        return this;
    }

    internal MotionBuilder ToFlip(
        BindableProperty property,
        object target,
        object invert,
        object visualFrom,
        object visualTo,
        double scaleX0,
        bool towardInvert = false)
    {
        _tweens.Add(new MotionTween(_view, property, target)
        {
            From = invert,
            VisualFrom = visualFrom,
            VisualTo = visualTo,
            ScaleX0 = scaleX0,
            TowardInvert = towardInvert,
        });
        return this;
    }

    public IMotionClip Build() => new MotionClip([.. _tweens], _duration, _easing, _owner);

    public Task PlayAsync(CancellationToken cancellationToken = default)
        => Build().PlayAsync(cancellationToken);
}

sealed class MotionTween(VisualElement view, BindableProperty property, object target)
{
    public VisualElement View { get; } = view;
    public BindableProperty Property { get; } = property;
    public object Target { get; } = target;
    public object? From { get; set; }
    public object? VisualFrom { get; set; }
    public object? VisualTo { get; set; }
    public double ScaleX0 { get; set; } = 1;
    public bool TowardInvert { get; set; }
}

sealed class MotionClip : IMotionClip
{
    readonly MotionTween[] _tweens;
    readonly uint _duration;
    readonly Easing _easing;
    readonly VisualElement? _owner;
    readonly string _name = $"reactor-animate-{Guid.NewGuid():N}";
    Action? _settled;

    public MotionClip(MotionTween[] tweens, uint duration, Easing easing, VisualElement? owner)
    {
        _tweens = tweens;
        _duration = duration;
        _easing = easing;
        _owner = owner;
    }

    public void NotifyWhenSettled(Action action) => _settled = action;

    public Task PlayAsync(CancellationToken cancellationToken = default)
        => Commit(forward: true, cancellationToken);

    public Task ReverseAsync(CancellationToken cancellationToken = default)
        => Commit(forward: false, cancellationToken);

    Task Commit(bool forward, CancellationToken cancellationToken)
    {
        if (_tweens.Length == 0)
        {
            if (!forward)
                _settled?.Invoke();
            return Task.CompletedTask;
        }

        foreach (var tween in _tweens)
        {
            if (forward)
            {
                tween.From ??= tween.View.GetValue(tween.Property);
                Write(tween, tween.From);
            }
        }

        var parent = new Animation();
        foreach (var tween in _tweens)
        {
            var from = tween.From ?? tween.View.GetValue(tween.Property);
            var to = tween.Target;
            if (!forward)
                (from, to) = (to, from);

            var animation = CreateAnimation(tween, from, to, forward);
            parent.Add(0, 1, animation);
        }

        var owner = _owner ?? _tweens[0].View;
        var tcs = new TaskCompletionSource<bool>();
        using var registration = cancellationToken.Register(() =>
        {
            owner.AbortAnimation(_name);
            tcs.TrySetCanceled(cancellationToken);
        });

        parent.Commit(
            owner,
            _name,
            16u,
            _duration,
            _easing,
            finished: (_, canceled) =>
            {
                if (!canceled)
                    ApplyEnds(forward);
                if (!forward)
                    _settled?.Invoke();
                tcs.TrySetResult(!canceled);
            });

        return tcs.Task;
    }

    void ApplyEnds(bool forward)
    {
        foreach (var tween in _tweens)
        {
            var value = forward ? tween.Target : tween.From ?? tween.Target;
            Write(tween, value);
        }
    }

    static Animation CreateAnimation(MotionTween tween, object from, object to, bool forward)
    {
        return new Animation(t =>
        {
            object? value;
            if (tween.VisualFrom is not null && tween.VisualTo is not null && tween.ScaleX0 is > 0 and not 1)
            {
                object visualFrom;
                object visualTo;
                double scaleFrom;
                double scaleTo;
                if (tween.TowardInvert)
                {
                    visualFrom = tween.VisualFrom;
                    visualTo = tween.VisualTo;
                    scaleFrom = 1d;
                    scaleTo = tween.ScaleX0;
                }
                else if (forward)
                {
                    visualFrom = tween.VisualFrom;
                    visualTo = tween.VisualTo;
                    scaleFrom = tween.ScaleX0;
                    scaleTo = 1d;
                }
                else
                {
                    visualFrom = tween.VisualTo;
                    visualTo = tween.VisualFrom;
                    scaleFrom = 1d;
                    scaleTo = tween.ScaleX0;
                }

                var scale = scaleFrom + (scaleTo - scaleFrom) * t;
                if (scale == 0)
                    scale = 1;
                var visual = PropertyFlip.Lerp(visualFrom, visualTo, t);
                value = visual is null ? from : PropertyFlip.ScaleLength(visual, 1 / scale);
            }
            else
            {
                value = PropertyFlip.Lerp(from, to, t) ?? to;
            }

            Write(tween, value);
        }, 0, 1);
    }

    static void Write(MotionTween tween, object? value)
    {
        if (value is null)
            return;

        if (tween.Property == Border.StrokeShapeProperty)
        {
            tween.View.SetValue(tween.Property, new RoundRectangle { CornerRadius = PropertyFlip.RadiusOf(value) });
            PropertyFlip.Push(tween.View, tween.Property);
            return;
        }

        tween.View.SetValue(tween.Property, value);
        PropertyFlip.Push(tween.View, tween.Property);
    }
}

sealed class ParallelClip(IReadOnlyList<IMotionClip> clips) : IMotionClip
{
    Action? _settled;

    public void NotifyWhenSettled(Action action) => _settled = action;

    public Task PlayAsync(CancellationToken cancellationToken = default)
        => Task.WhenAll(clips.Select(c => c.PlayAsync(cancellationToken)));

    public Task ReverseAsync(CancellationToken cancellationToken = default)
    {
        if (clips.Count == 0)
        {
            _settled?.Invoke();
            return Task.CompletedTask;
        }

        if (_settled is { } settled)
        {
            var remaining = clips.Count;
            foreach (var clip in clips)
            {
                clip.NotifyWhenSettled(() =>
                {
                    if (Interlocked.Decrement(ref remaining) == 0)
                        settled();
                });
            }
        }

        return Task.WhenAll(clips.Select(clip => clip.ReverseAsync(cancellationToken)));
    }
}
