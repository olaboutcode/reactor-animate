namespace Reactor.Animate;

/// <summary>
/// Playable motion clip. Push plays forward (0→1); pop reverses the same clip (1→0).
/// </summary>
public interface IMotionClip
{
    Task PlayAsync(CancellationToken cancellationToken = default);

    Task ReverseAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Fluent tween builder for visual elements. Navigation recipes compose clips from this API.
/// </summary>
public static class Motion
{
    public const uint DefaultDuration = 400;

    public static Easing DefaultEasing { get; } = Easing.CubicOut;

    public static MotionBuilder On(VisualElement view) => new(view);

    public static IMotionClip Parallel(params IMotionClip[] clips) => new ParallelClip(clips);
}

public sealed class MotionBuilder
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

    public MotionBuilder To(BindableProperty property, object target)
    {
        _tweens.Add(new MotionTween(_view, property, target));
        return this;
    }

    public IMotionClip Build() => new MotionClip(_tweens.ToArray(), _duration, _easing, _owner);

    public Task PlayAsync(CancellationToken cancellationToken = default)
        => Build().PlayAsync(cancellationToken);
}

sealed class MotionTween(VisualElement view, BindableProperty property, object target)
{
    public VisualElement View { get; } = view;
    public BindableProperty Property { get; } = property;
    public object Target { get; } = target;
    public object? From { get; set; }
}

sealed class MotionClip : IMotionClip
{
    readonly MotionTween[] _tweens;
    readonly uint _duration;
    readonly Easing _easing;
    readonly VisualElement? _owner;
    readonly string _name = $"reactor-animate-{Guid.NewGuid():N}";

    public MotionClip(MotionTween[] tweens, uint duration, Easing easing, VisualElement? owner)
    {
        _tweens = tweens;
        _duration = duration;
        _easing = easing;
        _owner = owner;
    }

    public Task PlayAsync(CancellationToken cancellationToken = default)
        => Commit(forward: true, cancellationToken);

    public Task ReverseAsync(CancellationToken cancellationToken = default)
        => Commit(forward: false, cancellationToken);

    Task Commit(bool forward, CancellationToken cancellationToken)
    {
        if (_tweens.Length == 0)
            return Task.CompletedTask;

        foreach (var tween in _tweens)
        {
            if (forward)
                tween.From = tween.View.GetValue(tween.Property);
        }

        var parent = new Animation();
        foreach (var tween in _tweens)
        {
            var from = tween.From ?? tween.View.GetValue(tween.Property);
            var to = tween.Target;
            if (!forward)
                (from, to) = (to, from);

            var animation = CreateAnimation(tween, from, to);
            if (animation is not null)
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
                tcs.TrySetResult(!canceled);
            });

        return tcs.Task;
    }

    void ApplyEnds(bool forward)
    {
        foreach (var tween in _tweens)
        {
            var value = forward ? tween.Target : tween.From ?? tween.Target;
            tween.View.SetValue(tween.Property, value);
        }
    }

    static Animation? CreateAnimation(MotionTween tween, object from, object to)
    {
        var view = tween.View;
        var property = tween.Property;

        if (property.ReturnType == typeof(double))
        {
            var start = Convert.ToDouble(from);
            var end = Convert.ToDouble(to);
            return new Animation(t => view.SetValue(property, start + (end - start) * t), 0, 1);
        }

        if (property.ReturnType == typeof(float))
        {
            var start = Convert.ToSingle(from);
            var end = Convert.ToSingle(to);
            return new Animation(t => view.SetValue(property, start + (end - start) * t), 0, 1);
        }

        if (property.ReturnType == typeof(Thickness))
        {
            var start = from is Thickness thickness ? thickness : new Thickness(Convert.ToDouble(from));
            var end = to is Thickness endThickness ? endThickness : new Thickness(Convert.ToDouble(to));
            return new Animation(t => view.SetValue(property, new Thickness(
                start.Left + (end.Left - start.Left) * t,
                start.Top + (end.Top - start.Top) * t,
                start.Right + (end.Right - start.Right) * t,
                start.Bottom + (end.Bottom - start.Bottom) * t)), 0, 1);
        }

        if (property.ReturnType == typeof(Color))
        {
            var start = (from as Color) ?? Colors.Transparent;
            var end = (to as Color) ?? Colors.Transparent;
            return new Animation(t => view.SetValue(property, Color.FromRgba(
                start.Red + (end.Red - start.Red) * t,
                start.Green + (end.Green - start.Green) * t,
                start.Blue + (end.Blue - start.Blue) * t,
                start.Alpha + (end.Alpha - start.Alpha) * t)), 0, 1);
        }

        if (property == AbsoluteLayout.LayoutBoundsProperty || property.ReturnType == typeof(Rect))
        {
            var start = from is Rect startRect ? startRect : default;
            var end = to is Rect endRect ? endRect : default;
            return new Animation(t => view.SetValue(property, new Rect(
                start.X + (end.X - start.X) * t,
                start.Y + (end.Y - start.Y) * t,
                start.Width + (end.Width - start.Width) * t,
                start.Height + (end.Height - start.Height) * t)), 0, 1);
        }

        throw new NotSupportedException($"Property '{property.PropertyName}' of type '{property.ReturnType.Name}' cannot be animated by Reactor.Animate.");
    }
}

sealed class ParallelClip(IReadOnlyList<IMotionClip> clips) : IMotionClip
{
    public Task PlayAsync(CancellationToken cancellationToken = default)
        => Task.WhenAll(clips.Select(c => c.PlayAsync(cancellationToken)));

    public Task ReverseAsync(CancellationToken cancellationToken = default)
        => Task.WhenAll(clips.Select(c => c.ReverseAsync(cancellationToken)));
}
