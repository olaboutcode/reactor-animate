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

    internal MotionPlayer(Motion motion, IReadOnlyList<VisualElement> targets, MotionClock? clock = null)
    {
        Motion = motion ?? throw new ArgumentNullException(nameof(motion));
        _targets = targets as VisualElement[] ?? [.. targets];
        _clock = clock ?? new MotionClock();
        _clock.Connect(OnDelta);
        _runtimes = BuildRuntimes(motion, _targets);
        Duration = motion.Duration;
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
                CancelPending();
                return StartRun(MotionPlaybackStatus.Forward, recapture: false, fireStarted: true, cancellationToken);
            default:
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
                CancelPending();
                return StartRun(MotionPlaybackStatus.Reverse, recapture: false, fireStarted: true, cancellationToken);
            default:
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
        => SeekFraction(Duration == 0 ? 1 : milliseconds / (double)Duration);

    public void SeekFraction(double u)
    {
        ThrowIfDisposed();
        u = double.IsNaN(u) || double.IsInfinity(u) ? 0 : Math.Clamp(u, 0, 1);
        _elapsedMs = u * Duration;
        Apply(u);
        ReportProgress();

        if (u >= 1)
            FinishForward();
        else if (u <= 0)
            FinishReverse(cancelPending: true);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _clock.Stop();
        CancelPending();
        _tokenReg.Dispose();
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
        }
        else if (Status == MotionPlaybackStatus.Completed && running == MotionPlaybackStatus.Reverse)
        {
            _elapsedMs = Duration;
        }

        _pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var task = _pending.Task;
        ArmToken(cancellationToken);
        SetStatus(running);
        _runArgs = NewArgs();
        if (fireStarted)
            Raise(Started);
        Apply(LinearU);
        ReportProgress();

        if (cancellationToken.IsCancellationRequested)
        {
            CancelToPaused();
            return task;
        }

        if (Duration == 0)
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

    void FinishForward()
    {
        _clock.Stop();
        _elapsedMs = Duration;
        Apply(1);
        SetStatus(MotionPlaybackStatus.Completed);
        ReportProgress();
        Raise(Completed);
        CompletePending();
    }

    void FinishReverse(bool cancelPending)
    {
        _clock.Stop();
        _elapsedMs = 0;
        Apply(0);
        SetStatus(MotionPlaybackStatus.Dismissed);
        ReportProgress();
        if (cancelPending)
            CancelPending();
        else
            CompletePending();
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
        var progress = Motion.Easing.Ease(LinearU);
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

    static TrackRuntime[] BuildRuntimes(Motion motion, VisualElement[] targets)
    {
        if (targets.Length == 0 || motion.Tracks.Count == 0)
            return [];

        var list = new List<TrackRuntime>(targets.Length * motion.Tracks.Count);
        foreach (var target in targets)
        {
            foreach (var track in motion.Tracks)
            {
                var property = ResolveProperty(track, target);
                if (property is null)
                    continue;
                list.Add(new TrackRuntime(
                    new WeakReference<VisualElement>(target),
                    property,
                    track.From,
                    track.To,
                    track.Begin,
                    track.End,
                    track.Easing ?? motion.Easing));
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
