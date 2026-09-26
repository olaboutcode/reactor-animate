using Microsoft.Maui.Controls.Shapes;
using Reactor.Animate.Page;
using MauiAnimation = Microsoft.Maui.Controls.Animation;

namespace Reactor.Animate.Animation;

internal interface ITweenClip
{
    Task PlayAsync(Action<double>? onProgress = null, CancellationToken cancellationToken = default);
}

internal static class Tween
{
    public const uint DefaultDuration = 400;

    public static Easing DefaultEasing { get; } = Easing.CubicOut;

    public static TweenBuilder On(VisualElement view) => new(view);
}

internal sealed class TweenBuilder
{
    readonly List<TweenStep> _tweens = [];
    VisualElement _view;
    uint _duration = Tween.DefaultDuration;
    Easing _easing = Tween.DefaultEasing;
    VisualElement? _owner;
    double _begin;

    internal TweenBuilder(VisualElement view) => _view = view;

    public TweenBuilder On(VisualElement view)
    {
        _view = view;
        _begin = 0;
        return this;
    }

    public TweenBuilder Owner(VisualElement owner)
    {
        _owner = owner;
        return this;
    }

    public TweenBuilder Duration(uint milliseconds)
    {
        _duration = milliseconds;
        return this;
    }

    public TweenBuilder Easing(Easing easing)
    {
        _easing = easing;
        return this;
    }

    public TweenBuilder Delay(double begin)
    {
        _begin = Math.Clamp(begin, 0, 1);
        return this;
    }

    public TweenBuilder To(BindableProperty property, object target, object? from = null)
    {
        _tweens.Add(new TweenStep(_view, property, target) { From = from, Begin = _begin });
        return this;
    }

    internal TweenBuilder ToFlip(
        BindableProperty property,
        object target,
        object invert,
        object visualFrom,
        object visualTo,
        double scaleX0)
    {
        _tweens.Add(new TweenStep(_view, property, target)
        {
            From = invert,
            VisualFrom = visualFrom,
            VisualTo = visualTo,
            ScaleX0 = scaleX0,
            Begin = _begin,
        });
        return this;
    }

    public ITweenClip Build() => new TweenClip([.. _tweens], _duration, _easing, _owner);

    internal bool HasTweens => _tweens.Count > 0;
}

internal sealed class TweenStep(VisualElement view, BindableProperty property, object target)
{
    public VisualElement View { get; } = view;
    public BindableProperty Property { get; } = property;
    public object Target { get; } = target;
    public object? From { get; set; }
    public object? VisualFrom { get; set; }
    public object? VisualTo { get; set; }
    public double ScaleX0 { get; set; } = 1;
    public double Begin { get; set; }
}

internal sealed class TweenClip(
    TweenStep[] tweens,
    uint duration,
    Easing easing,
    VisualElement? owner) : ITweenClip
{
    static int _nextName;

    readonly TweenStep[] _tweens = tweens;
    readonly uint _duration = duration;
    readonly Easing _easing = easing;
    readonly VisualElement? _owner = owner;
    readonly string _name = $"reactor-animate-{Interlocked.Increment(ref _nextName)}";

    public Task PlayAsync(Action<double>? onProgress = null, CancellationToken cancellationToken = default)
    {
        if (_tweens.Length == 0)
        {
            onProgress?.Invoke(1);
            return Task.CompletedTask;
        }

        foreach (var tween in _tweens)
        {
            tween.From ??= tween.View.GetValue(tween.Property);
            Write(tween, tween.From);
        }

        var parent = new MauiAnimation();
        foreach (var tween in _tweens)
        {
            var from = tween.From ?? tween.View.GetValue(tween.Property);
            parent.Add(tween.Begin, 1, CreateAnimation(tween, from, tween.Target));
        }

        if (onProgress is not null)
            parent.Add(0, 1, new MauiAnimation(t => onProgress(t)));

        var clipOwner = _owner ?? _tweens[0].View;
        var tcs = new TaskCompletionSource<bool>();
        CancellationTokenRegistration registration = default;
        if (cancellationToken.CanBeCanceled)
        {
            registration = cancellationToken.Register(() =>
            {
                clipOwner.AbortAnimation(_name);
                tcs.TrySetCanceled(cancellationToken);
            });
        }

        parent.Commit(
            clipOwner,
            _name,
            16u,
            _duration,
            _easing,
            finished: (_, canceled) =>
            {
                registration.Dispose();
                if (!canceled)
                    ApplyEnds();
                onProgress?.Invoke(1);
                tcs.TrySetResult(!canceled);
            });

        return tcs.Task;
    }

    void ApplyEnds()
    {
        foreach (var tween in _tweens)
            Write(tween, tween.Target);
    }

    static MauiAnimation CreateAnimation(TweenStep tween, object from, object to)
    {
        return new MauiAnimation(t =>
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

    static void Write(TweenStep tween, object? value)
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
        if (NeedsHandlerPush(tween.Property))
            PropertyFlip.Push(tween.View, tween.Property);
    }

    static bool NeedsHandlerPush(BindableProperty property)
        => property == Border.StrokeShapeProperty
            || property == BoxView.CornerRadiusProperty
            || property.PropertyName is "CornerRadius" or "StrokeShape";
}
