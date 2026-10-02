using Reactor.Animate;

namespace Reactor.Animate.Internals;

/// <summary>
/// One shared-element clip. <c>PlayAsync</c> runs from the invert to the rest pose.
/// A pop does not reverse this clip. <see cref="HeroNavigation"/> builds a new one
/// from the destination frames and negated extras.
/// </summary>
internal interface IFlipClip
{
    Task PlayAsync(Action<double>? onProgress = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Starts a <see cref="FlipTweenBuilder"/> on a view. <see cref="HeroNavigation"/>
/// builds one builder for the page and adds a step per flying property.
/// </summary>
internal static class FlipTween
{
    public static FlipTweenBuilder On(VisualElement view) => new(view);
}

/// <summary>
/// Property steps for one flight. <c>Delay</c> is a fraction of the clip, from 0 to 1,
/// not milliseconds. Chrome fade uses that to start after the shared elements are
/// more than halfway through.
/// </summary>
internal sealed class FlipTweenBuilder
{
    readonly List<FlipStep> _tweens = [];
    VisualElement _view;
    uint _duration = Timing.PageDuration;
    Easing _easing = Timing.PageEasing;
    VisualElement? _owner;
    double _begin;

    internal FlipTweenBuilder(VisualElement view) => _view = view;

    public FlipTweenBuilder On(VisualElement view)
    {
        _view = view;
        _begin = 0;
        return this;
    }

    public FlipTweenBuilder Owner(VisualElement owner)
    {
        _owner = owner;
        return this;
    }

    public FlipTweenBuilder Duration(uint milliseconds)
    {
        _duration = milliseconds;
        return this;
    }

    public FlipTweenBuilder Easing(Easing easing)
    {
        _easing = easing;
        return this;
    }

    public FlipTweenBuilder Delay(double begin)
    {
        _begin = Math.Clamp(begin, 0, 1);
        return this;
    }

    public FlipTweenBuilder To(BindableProperty property, object target, object? from = null)
    {
        _tweens.Add(new FlipStep(_view, property, target) { From = from, Begin = _begin });
        return this;
    }

    internal FlipTweenBuilder ToFlip(
        BindableProperty property,
        object target,
        object invert,
        object visualFrom,
        object visualTo,
        double scaleX0)
    {
        _tweens.Add(new FlipStep(_view, property, target)
        {
            From = invert,
            VisualFrom = visualFrom,
            VisualTo = visualTo,
            ScaleX0 = scaleX0,
            Begin = _begin,
        });
        return this;
    }

    public IFlipClip Build(MotionClock? clock = null)
        => new FlipClip([.. _tweens], _duration, _easing, _owner, clock);

    internal bool HasTweens => _tweens.Count > 0;
}

/// <summary>
/// One property on one view. <c>From</c> is written before the clip eases to <c>Target</c>.
/// <c>VisualFrom</c>, <c>VisualTo</c>, and <c>ScaleX0</c> keep length properties in
/// unscaled units while the flying view is scaled to the other frame.
/// </summary>
internal sealed class FlipStep(VisualElement view, BindableProperty property, object target)
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

/// <summary>
/// Plays <see cref="FlipStep"/>s on a <see cref="MotionClock"/>. Each step writes
/// its <c>From</c> immediately, then eases to <c>Target</c> over the clip duration.
/// </summary>
internal sealed class FlipClip(
    FlipStep[] tweens,
    uint duration,
    Easing easing,
    VisualElement? owner,
    MotionClock? clock) : IFlipClip
{
    readonly FlipStep[] _tweens = tweens;
    readonly uint _duration = duration;
    readonly Easing _easing = easing;
    readonly VisualElement? _owner = owner;
    readonly MotionClock _clock = clock ?? new MotionClock();
    readonly TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Action<double>? _onProgress;
    CancellationTokenRegistration _tokenReg;
    double _elapsedMs;
    int _done;

    public Task PlayAsync(Action<double>? onProgress = null, CancellationToken cancellationToken = default)
    {
        if (_tweens.Length == 0)
        {
            onProgress?.Invoke(1);
            return Task.CompletedTask;
        }

        _onProgress = onProgress;
        _clock.Connect(OnDelta);

        foreach (var tween in _tweens)
        {
            tween.From ??= tween.View.GetValue(tween.Property);
            PropertyLerp.Write(tween.View, tween.Property, tween.From);
        }

        if (cancellationToken.CanBeCanceled)
        {
            _tokenReg = cancellationToken.Register(() => Finish(canceled: true));
            if (cancellationToken.IsCancellationRequested)
            {
                Finish(canceled: true);
                return _tcs.Task;
            }
        }

        if (_duration == 0)
        {
            Apply(1);
            Finish(canceled: false);
            return _tcs.Task;
        }

        var owner = _owner ?? _tweens[0].View;
        _clock.Start([owner]);
        if (!_clock.IsPumping)
        {
            Apply(1);
            Finish(canceled: false);
        }

        return _tcs.Task;
    }

    void OnDelta(double deltaMs)
    {
        if (Volatile.Read(ref _done) != 0)
            return;

        if (double.IsPositiveInfinity(deltaMs) || _duration == 0)
        {
            Apply(1);
            Finish(canceled: false);
            return;
        }

        _elapsedMs += deltaMs;
        if (_elapsedMs >= _duration)
        {
            Apply(1);
            Finish(canceled: false);
            return;
        }

        Apply(Math.Clamp(_elapsedMs / _duration, 0, 1));
    }

    void Apply(double linearU)
    {
        var eased = _easing.Ease(linearU);
        _onProgress?.Invoke(eased);
        foreach (var tween in _tweens)
            WriteStep(tween, eased);
    }

    static void WriteStep(FlipStep tween, double eased)
    {
        var local = LocalT(tween.Begin, eased);
        var from = tween.From ?? tween.View.GetValue(tween.Property);
        object? value;
        if (tween.VisualFrom is not null && tween.VisualTo is not null && tween.ScaleX0 is > 0 and not 1)
        {
            var scale = tween.ScaleX0 + (1 - tween.ScaleX0) * local;
            if (scale == 0)
                scale = 1;
            var visual = PropertyLerp.Lerp(tween.VisualFrom, tween.VisualTo, local);
            value = visual is null ? from : PropertyLerp.ScaleLength(visual, 1 / scale);
        }
        else
        {
            value = PropertyLerp.Lerp(from, tween.Target, local) ?? tween.Target;
        }

        PropertyLerp.Write(tween.View, tween.Property, value);
    }

    static double LocalT(double begin, double eased)
    {
        if (begin >= 1)
            return eased >= 1 ? 1 : 0;
        if (eased <= begin)
            return 0;
        return (eased - begin) / (1 - begin);
    }

    void Finish(bool canceled)
    {
        if (Interlocked.Exchange(ref _done, 1) != 0)
            return;

        _clock.Stop();
        _tokenReg.Dispose();
        if (!canceled)
        {
            foreach (var tween in _tweens)
                PropertyLerp.Write(tween.View, tween.Property, tween.Target);
            _onProgress?.Invoke(1);
            _tcs.TrySetResult(true);
            return;
        }

        _tcs.TrySetCanceled();
    }
}
