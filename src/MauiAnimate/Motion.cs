using Microsoft.Maui.Controls.Shapes;

namespace Reactor.Animate;

interface IMotionClip
{
    Task PlayAsync(CancellationToken cancellationToken = default);
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
    double _begin;

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

    public MotionBuilder Delay(double begin)
    {
        _begin = Math.Clamp(begin, 0, 1);
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
        double scaleX0)
    {
        _tweens.Add(new MotionTween(_view, property, target)
        {
            From = invert,
            VisualFrom = visualFrom,
            VisualTo = visualTo,
            ScaleX0 = scaleX0,
        });
        return this;
    }

    public IMotionClip Build() => new MotionClip([.. _tweens], _duration, _easing, _owner, _begin);

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
}

sealed class MotionClip(
    MotionTween[] tweens,
    uint duration,
    Easing easing,
    VisualElement? owner,
    double begin = 0) : IMotionClip
{
    readonly MotionTween[] _tweens = tweens;
    readonly uint _duration = duration;
    readonly Easing _easing = easing;
    readonly VisualElement? _owner = owner;
    readonly double _begin = begin;
    readonly string _name = $"reactor-animate-{Guid.NewGuid():N}";

    public Task PlayAsync(CancellationToken cancellationToken = default)
    {
        if (_tweens.Length == 0)
            return Task.CompletedTask;

        foreach (var tween in _tweens)
        {
            tween.From ??= tween.View.GetValue(tween.Property);
            Write(tween, tween.From);
        }

        var parent = new Animation();
        foreach (var tween in _tweens)
        {
            var from = tween.From ?? tween.View.GetValue(tween.Property);
            parent.Add(_begin, 1, CreateAnimation(tween, from, tween.Target));
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
                    ApplyEnds();
                tcs.TrySetResult(!canceled);
            });

        return tcs.Task;
    }

    void ApplyEnds()
    {
        foreach (var tween in _tweens)
            Write(tween, tween.Target);
    }

    static Animation CreateAnimation(MotionTween tween, object from, object to)
    {
        return new Animation(t =>
        {
            object? value;
            if (tween.VisualFrom is not null && tween.VisualTo is not null && tween.ScaleX0 is > 0 and not 1)
            {
                var scale = tween.ScaleX0 + (1 - tween.ScaleX0) * t;
                if (scale == 0)
                    scale = 1;
                var visual = PropertyFlip.Lerp(tween.VisualFrom, tween.VisualTo, t);
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
    public Task PlayAsync(CancellationToken cancellationToken = default)
        => Task.WhenAll(clips.Select(clip => clip.PlayAsync(cancellationToken)));
}
