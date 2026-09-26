using Reactor.Animate.Motion;

namespace Reactor.Animate.Animation;

public sealed class MotionPlayer : IDisposable
{
    readonly VisualElement[] _targets;
    readonly TrackRuntime[] _runtimes;
    readonly MotionClock _clock;
    readonly List<Action<double>> _at = [];
    readonly object _gate = new();

    TaskCompletionSource<bool>? _pending;
    CancellationTokenRegistration _tokenReg;
    MotionPlaybackEventArgs? _runArgs;
    MotionPlaybackStatus _pausedWas;
    bool _captured;
    bool _disposed;
    double _elapsedMs;
    int _direction = 1;
    int _repeatRemaining;
    bool _autoRepeat = true;
    double _x;
    double _v;
    double _springElapsedMs;

    internal MotionPlayer(Motion motion, IReadOnlyList<VisualElement> targets, MotionClock? clock = null)
    {
        Motion = motion ?? throw new ArgumentNullException(nameof(motion));
        _targets = targets as VisualElement[] ?? [.. targets];
        _clock = clock ?? new MotionClock();
        _clock.Connect(OnDelta);
        var parentSpan = motion.Duration;
        var maxDelay = motion.StaggerSpec is { } stagger && _targets.Length > 1 && parentSpan > 0
            ? StaggerEval.MaxDelayMs(_targets.Length, stagger)
            : 0;
        Duration = parentSpan == 0 ? 0 : parentSpan + (uint)Math.Round(maxDelay);
        _runtimes = BuildRuntimes(motion, _targets, parentSpan, Duration);
        foreach (var target in _targets)
            MotionPlayers.Register(target, this);
    }

    internal static MotionPlayer Create(
        Motion motion,
        IReadOnlyList<VisualElement> targets,
        MotionClock? clock = null)
        => new(motion, targets, clock);

    public Motion Motion { get; }

    public MotionPlaybackStatus Status { get; private set; } = MotionPlaybackStatus.Dismissed;

    public double Progress { get; private set; }

    public bool IsRunning
        => Status is MotionPlaybackStatus.Forward or MotionPlaybackStatus.Reverse;

    public uint Duration { get; }

    public event EventHandler<MotionPlaybackEventArgs>? Started;
    public event EventHandler<MotionPlaybackEventArgs>? Completed;
    public event EventHandler<MotionPlaybackEventArgs>? Paused;
    public event EventHandler<MotionPlaybackEventArgs>? Resumed;
    public event EventHandler<MotionPlaybackEventArgs>? StatusChanged;

    public void At(Action<double> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        lock (_gate)
            _at.Add(callback);
        MotionPlaybackEventArgs.Invoke(callback, Progress);
    }

    public Task ForwardAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        switch (Status)
        {
            case MotionPlaybackStatus.Completed:
                return Task.CompletedTask;
            case MotionPlaybackStatus.Forward when HasLivePending:
                return _pending!.Task;
            case MotionPlaybackStatus.Paused when _pausedWas == MotionPlaybackStatus.Forward && HasLivePending:
                ResumeRunning();
                return _pending!.Task;
            case MotionPlaybackStatus.Paused when _pausedWas == MotionPlaybackStatus.Forward:
                return StartRun(MotionPlaybackStatus.Forward, recapture: false, fireStarted: true, cancellationToken);
            case MotionPlaybackStatus.Reverse:
            case MotionPlaybackStatus.Paused when _pausedWas == MotionPlaybackStatus.Reverse:
                _autoRepeat = true;
                SupersedePending();
                return StartRun(MotionPlaybackStatus.Forward, recapture: false, fireStarted: true, cancellationToken);
            default:
                _autoRepeat = true;
                return StartRun(MotionPlaybackStatus.Forward, recapture: true, fireStarted: true, cancellationToken);
        }
    }

    public Task ReverseAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        switch (Status)
        {
            case MotionPlaybackStatus.Dismissed:
                return Task.CompletedTask;
            case MotionPlaybackStatus.Reverse when HasLivePending:
                return _pending!.Task;
            case MotionPlaybackStatus.Paused when _pausedWas == MotionPlaybackStatus.Reverse && HasLivePending:
                ResumeRunning();
                return _pending!.Task;
            case MotionPlaybackStatus.Paused when _pausedWas == MotionPlaybackStatus.Reverse:
                return StartRun(MotionPlaybackStatus.Reverse, recapture: false, fireStarted: true, cancellationToken);
            case MotionPlaybackStatus.Forward:
            case MotionPlaybackStatus.Paused when _pausedWas == MotionPlaybackStatus.Forward:
                _autoRepeat = false;
                SupersedePending();
                return StartRun(MotionPlaybackStatus.Reverse, recapture: false, fireStarted: true, cancellationToken);
            default:
                _autoRepeat = false;
                return StartRun(MotionPlaybackStatus.Reverse, recapture: false, fireStarted: true, cancellationToken);
        }
    }

    public void Pause()
    {
        ThrowIfDisposed();
        if (!IsRunning)
            return;
        _clock.Stop();
        _pausedWas = Status;
        SetStatus(MotionPlaybackStatus.Paused);
        Raise(Paused);
    }

    public void Resume()
    {
        ThrowIfDisposed();
        if (Status != MotionPlaybackStatus.Paused)
            return;

        if (HasLivePending)
        {
            ResumeRunning();
            return;
        }

        _ = StartRun(_pausedWas, recapture: false, fireStarted: true, CancellationToken.None);
    }

    public void Reset()
    {
        ThrowIfDisposed();
        _clock.Stop();
        CancelPending();
        _elapsedMs = 0;
        _direction = 1;
        if (_captured)
        {
            foreach (var runtime in _runtimes)
                runtime.WriteFrom();
        }

        SetStatus(MotionPlaybackStatus.Dismissed);
        ReportProgress();
    }

    public void Seek(uint milliseconds)
    {
        ThrowIfDisposed();
        var u = Duration == 0 ? 1 : milliseconds / (double)Duration;
        SeekLinear(u);
    }

    /// <summary>
    /// Seeks to the start of a named timeline child. Position is linear player
    /// time (same as <see cref="Seek(uint)"/>), not eased <see cref="SeekFraction"/>.
    /// </summary>
    public void Seek(string id)
    {
        ThrowIfDisposed();
        if (!TrySpan(id, out var begin, out _))
            return;
        SeekLinear(begin);
    }

    /// <summary>
    /// Linear 0–1 of <see cref="Duration"/> (player span after stagger) for a
    /// named <see cref="Motion.Add(Motion, uint, string?)"/> / <see cref="Motion.Then(Motion, string?)"/> child.
    /// </summary>
    public bool TrySpan(string id, out double begin, out double end)
    {
        begin = 0;
        end = 0;
        if (string.IsNullOrWhiteSpace(id))
            return false;

        foreach (var span in Motion.NamedSpans)
        {
            if (span.Id != id)
                continue;

            var parent = Motion.Duration;
            if (parent == 0 || Duration == 0)
            {
                begin = span.Begin;
                end = span.End;
                return true;
            }

            begin = span.Begin * parent / Duration;
            end = span.End * parent / Duration;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Seeks to eased progress <paramref name="t"/> (same units as
    /// <see cref="Progress"/>). Leaves the player <see cref="MotionPlaybackStatus.Paused"/>
    /// for <c>0 &lt; t &lt; 1</c>. Does not start playback. <see cref="Seek(uint)"/> is linear wall-clock.
    /// </summary>
    public void SeekFraction(double t)
    {
        ThrowIfDisposed();
        t = double.IsNaN(t) || double.IsInfinity(t) ? 0 : Math.Clamp(t, 0, 1);
        EasingInvert.TryInvert(Motion.Easing, t, out var u);
        SeekLinear(u);
    }

    void SeekLinear(double u)
    {
        u = double.IsNaN(u) || double.IsInfinity(u) ? 0 : Math.Clamp(u, 0, 1);
        _autoRepeat = false;
        _clock.Stop();
        _elapsedMs = u * Duration;
        ResetSpring(u);
        Apply(u);

        if (u >= 1)
        {
            FinishForward();
            return;
        }

        if (u <= 0)
        {
            FinishReverse(cancelPending: true);
            return;
        }

        PauseForSeek();
        ReportProgress();
    }

    void PauseForSeek()
    {
        if (IsRunning)
        {
            _pausedWas = Status;
            SetStatus(MotionPlaybackStatus.Paused);
            Raise(Paused);
            return;
        }

        if (Status is MotionPlaybackStatus.Dismissed or MotionPlaybackStatus.Completed)
        {
            _pausedWas = MotionPlaybackStatus.Forward;
            SetStatus(MotionPlaybackStatus.Paused);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _clock.Stop();
        CancelPending();
        _tokenReg.Dispose();
        foreach (var target in _targets)
            MotionPlayers.Unregister(target, this);
    }

    bool HasLivePending => _pending is { Task.IsCompleted: false };

    Task StartRun(
        MotionPlaybackStatus running,
        bool recapture,
        bool fireStarted,
        CancellationToken cancellationToken)
    {
        if (_targets.Length == 0)
        {
            SetStatus(running == MotionPlaybackStatus.Forward
                ? MotionPlaybackStatus.Completed
                : MotionPlaybackStatus.Dismissed);
            return Task.CompletedTask;
        }

        _direction = running == MotionPlaybackStatus.Forward ? 1 : -1;
        if (recapture)
        {
            foreach (var runtime in _runtimes)
                runtime.CaptureAndWriteFrom();
            _captured = true;
            _elapsedMs = 0;
            _repeatRemaining = Motion.RepeatCount < 0 ? -1 : Math.Max(1, Motion.RepeatCount);
            ResetSpring(_direction > 0 ? 0 : 1);
        }
        else if (Status == MotionPlaybackStatus.Completed && running == MotionPlaybackStatus.Reverse)
        {
            _elapsedMs = Duration;
            ResetSpring(1);
        }

        _pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var task = _pending.Task;
        ArmToken(cancellationToken);
        SetStatus(running);
        _runArgs = NewArgs();
        if (fireStarted)
            Raise(Started);
        Apply(PlaybackU);
        ReportProgress();

        if (cancellationToken.IsCancellationRequested)
        {
            CancelToPaused();
            return task;
        }

        if (Duration == 0 && Motion.Spring is null)
        {
            if (_direction > 0)
                FinishForward();
            else
                FinishReverse(cancelPending: false);
            return task;
        }

        _clock.Start(_targets);
        return task;
    }

    void ResumeRunning()
    {
        SetStatus(_pausedWas);
        _runArgs = _runArgs is null ? NewArgs() : _runArgs;
        _runArgs.Status = Status;
        Raise(Resumed);
        _clock.Start(_targets);
    }

    void OnDelta(double deltaMs)
    {
        if (_disposed || !IsRunning)
            return;

        if (Motion.Spring is { } spring)
        {
            StepSpring(deltaMs, spring);
            return;
        }

        if (double.IsPositiveInfinity(deltaMs) || Duration == 0)
        {
            if (_direction > 0)
                FinishForward();
            else
                FinishReverse(cancelPending: false);
            return;
        }

        _elapsedMs += _direction * deltaMs;
        if (_elapsedMs >= Duration)
        {
            _elapsedMs = Duration;
            Apply(1);
            ReportProgress();
            FinishForward();
            return;
        }

        if (_elapsedMs <= 0)
        {
            _elapsedMs = 0;
            Apply(0);
            ReportProgress();
            FinishReverse(cancelPending: false);
            return;
        }

        Apply(LinearU);
        ReportProgress();
    }

    void StepSpring(double deltaMs, Spring spring)
    {
        var target = _direction > 0 ? 1d : 0d;
        if (double.IsPositiveInfinity(deltaMs))
        {
            _x = target;
            _v = 0;
            Apply(target);
            ReportProgress();
            if (_direction > 0)
                FinishForward();
            else
                FinishReverse(cancelPending: false);
            return;
        }

        var dt = Math.Min(Math.Max(deltaMs, 0) / 1000d, 1d / 30d);
        var mass = Math.Max(spring.Mass, 1e-6);
        var stiffness = Math.Max(spring.Stiffness, 1e-6);
        var damping = Math.Max(spring.Damping, 0);
        var accel = (-stiffness * (_x - target) - damping * _v) / mass;
        _v += accel * dt;
        _x += _v * dt;
        _springElapsedMs += deltaMs;

        Apply(PlaybackU);
        ReportProgress();

        if (Math.Abs(_x - target) < 0.002 && Math.Abs(_v) < 0.002 || _springElapsedMs > 8000)
        {
            _x = target;
            _v = 0;
            if (_direction > 0)
                FinishForward();
            else
                FinishReverse(cancelPending: false);
        }
    }

    void ResetSpring(double x)
    {
        _x = x;
        _v = 0;
        _springElapsedMs = 0;
    }

    double PlaybackU
        => Motion.Spring is null ? LinearU : Math.Clamp(_x, 0, 1);

    void FinishForward()
    {
        _clock.Stop();
        _elapsedMs = Duration;
        _x = 1;
        _v = 0;
        Apply(1);
        SetStatus(MotionPlaybackStatus.Completed);
        ReportProgress();
        Raise(Completed);
        if (_autoRepeat && TryContinueAfterForward())
            return;
        CompletePending();
    }

    void FinishReverse(bool cancelPending)
    {
        _clock.Stop();
        _elapsedMs = 0;
        _x = 0;
        _v = 0;
        Apply(0);
        SetStatus(MotionPlaybackStatus.Dismissed);
        ReportProgress();
        if (!cancelPending && _autoRepeat && TryContinueAfterReverse())
            return;
        if (cancelPending)
            CancelPending();
        else
            CompletePending();
    }

    bool TryContinueAfterForward()
    {
        if (_disposed || Duration == 0 && Motion.Spring is null)
            return false;
        if (Motion.YoyoEnabled)
        {
            ContinueReverse();
            return true;
        }

        if (!ConsumeRepeat())
            return false;
        ContinueForward();
        return true;
    }

    bool TryContinueAfterReverse()
    {
        if (_disposed || Duration == 0 && Motion.Spring is null || !Motion.YoyoEnabled)
            return false;
        if (!ConsumeRepeat())
            return false;
        ContinueForward();
        return true;
    }

    bool ConsumeRepeat()
    {
        if (_repeatRemaining < 0)
            return true;
        if (_repeatRemaining <= 1)
            return false;
        _repeatRemaining--;
        return true;
    }

    void ContinueForward()
    {
        _elapsedMs = 0;
        _direction = 1;
        ResetSpring(0);
        foreach (var runtime in _runtimes)
            runtime.WriteFrom();
        SetStatus(MotionPlaybackStatus.Forward);
        _runArgs = NewArgs();
        Raise(Started);
        Apply(0);
        ReportProgress();
        _clock.Start(_targets);
    }

    void ContinueReverse()
    {
        _elapsedMs = Duration;
        _direction = -1;
        ResetSpring(1);
        SetStatus(MotionPlaybackStatus.Reverse);
        _runArgs = NewArgs();
        Raise(Started);
        Apply(1);
        ReportProgress();
        _clock.Start(_targets);
    }

    void CancelToPaused()
    {
        if (!IsRunning && Status != MotionPlaybackStatus.Paused)
            return;
        _clock.Stop();
        if (IsRunning)
            _pausedWas = Status;
        SetStatus(MotionPlaybackStatus.Paused);
        CancelPending();
        Raise(Paused);
    }

    void Apply(double u)
    {
        foreach (var runtime in _runtimes)
            runtime.Apply(u);
    }

    double LinearU
        => Duration == 0
            ? (_direction > 0 ? 1 : 0)
            : Math.Clamp(_elapsedMs / Duration, 0, 1);

    void ReportProgress()
    {
        var progress = Motion.Spring is null
            ? Motion.Easing.Ease(LinearU)
            : PlaybackU;
        Progress = progress;
        if (_runArgs is not null)
        {
            _runArgs.Status = Status;
            _runArgs.Report(progress);
        }

        Action<double>[] listeners;
        lock (_gate)
            listeners = [.. _at];
        foreach (var callback in listeners)
            MotionPlaybackEventArgs.Invoke(callback, progress);
    }

    void SetStatus(MotionPlaybackStatus status)
    {
        if (Status == status)
            return;
        Status = status;
        if (_runArgs is not null)
            _runArgs.Status = status;
        Raise(StatusChanged);
    }

    MotionPlaybackEventArgs NewArgs()
        => new(Motion, _targets, Status, Progress);

    void Raise(EventHandler<MotionPlaybackEventArgs>? handler)
    {
        var args = _runArgs ?? NewArgs();
        if (handler is null)
            return;
        foreach (var candidate in handler.GetInvocationList())
        {
            try
            {
                ((EventHandler<MotionPlaybackEventArgs>)candidate)(this, args);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
    }

    void ArmToken(CancellationToken cancellationToken)
    {
        _tokenReg.Dispose();
        if (!cancellationToken.CanBeCanceled)
            return;
        _tokenReg = cancellationToken.Register(CancelToPaused);
    }

    void SupersedePending()
    {
        _tokenReg.Dispose();
        _pending?.TrySetResult(true);
        _pending = null;
    }

    void CancelPending()
    {
        _tokenReg.Dispose();
        _pending?.TrySetCanceled();
        _pending = null;
    }

    void CompletePending()
    {
        _tokenReg.Dispose();
        _pending?.TrySetResult(true);
        _pending = null;
    }

    void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, this);

    static TrackRuntime[] BuildRuntimes(
        Motion motion,
        VisualElement[] targets,
        uint parentSpan,
        uint playerSpan)
    {
        if (targets.Length == 0 || motion.Tracks.Count == 0)
            return [];

        var list = new List<TrackRuntime>(targets.Length * motion.Tracks.Count);
        for (var i = 0; i < targets.Length; i++)
        {
            var target = targets[i];
            var delay = motion.StaggerSpec is { } stagger && targets.Length > 1 && parentSpan > 0
                ? StaggerEval.DelayMs(i, targets.Length, stagger)
                : 0;
            foreach (var track in motion.Tracks)
            {
                var begin = playerSpan == 0 ? 0 : (delay + track.Begin * parentSpan) / playerSpan;
                var end = playerSpan == 0 ? 1 : (delay + track.End * parentSpan) / playerSpan;
                if (track.Semantic == SemanticTrack.Path)
                {
                    if (track.Path is null)
                        continue;
                    var sampler = PathSampler.TryCreate(track.Path);
                    if (sampler is null)
                        continue;
                    list.Add(new TrackRuntime(
                        new WeakReference<VisualElement>(target),
                        VisualElement.TranslationXProperty,
                        null,
                        track.To,
                        begin,
                        end,
                        track.Easing ?? motion.Easing,
                        path: sampler,
                        pathFrom: track.PathFrom,
                        pathTo: track.PathTo));
                    continue;
                }

                var property = ResolveProperty(track, target);
                if (property is null)
                    continue;
                list.Add(new TrackRuntime(
                    new WeakReference<VisualElement>(target),
                    property,
                    track.From,
                    track.To,
                    begin,
                    end,
                    track.Easing ?? motion.Easing,
                    track.Keyframes,
                    motion.ColorSpace));
            }
        }

        return [.. list];
    }

    static BindableProperty? ResolveProperty(MotionTrack track, VisualElement target)
    {
        if (track.Property is not null)
            return track.Property;

        if (track.Semantic != SemanticTrack.CornerRadius)
            return null;

        if (target is BoxView)
            return BoxView.CornerRadiusProperty;
        if (target is Border)
            return Border.StrokeShapeProperty;

        System.Diagnostics.Debug.WriteLine($"Motion.CornerRadius skipped on {target.GetType().Name}.");
        return null;
    }
}
