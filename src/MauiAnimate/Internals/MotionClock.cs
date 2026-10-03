using Reactor.Animate;
using Microsoft.Maui.Animations;

namespace Reactor.Animate.Internals;

/// <summary>
/// Frame pump shared by <see cref="MotionPlayer"/> and <see cref="FlipClip"/>.
/// Each tick reports delta milliseconds. A manual clock does not subscribe to the
/// platform animation manager; tests advance it with <c>Tick</c>.
/// The dispatcher timer is created once and restarted. <see cref="IsPumping"/>
/// is true for a manual clock, or while that timer is running. A stopped timer
/// is not pumping, so a flight that could not start still finishes.
/// </summary>
internal sealed class MotionClock
{
    readonly bool _manual;
    Action<double>? _onDelta;
    IAnimationManager? _manager;
    IDispatcherTimer? _timer;
    bool _running;
    bool _pumping;
    bool _loggedEnergySaver;
    long _lastTicks;

    public MotionClock(bool manual = false)
    {
        _manual = manual;
    }

    public void Connect(Action<double> onDelta)
        => _onDelta = onDelta ?? throw new ArgumentNullException(nameof(onDelta));

    public bool IsRunning => _running;

    public bool IsPumping => _manual || _pumping;

    public void Start(IEnumerable<VisualElement> targets)
    {
        if (_running)
            return;
        _running = true;
        _pumping = false;
        if (_manual)
            return;

        foreach (var target in targets)
        {
            _manager = target.Handler?.MauiContext?.Services.GetService(typeof(IAnimationManager)) as IAnimationManager;
            if (_manager is not null)
                break;
        }

        if (_manager is { SpeedModifier: 0 })
        {
            _onDelta?.Invoke(double.PositiveInfinity);
            return;
        }

        if (_manager?.Ticker is { SystemEnabled: false })
        {
            if (!_loggedEnergySaver)
            {
                _loggedEnergySaver = true;
                System.Diagnostics.Debug.WriteLine("MotionClock: animation ticker disabled; freezing.");
            }

            return;
        }

        if (Application.Current?.Dispatcher is not { } dispatcher)
            return;

        _lastTicks = Environment.TickCount64;
        if (_timer is null)
        {
            _timer = dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(16);
            _timer.Tick += OnTick;
        }

        _timer.Start();
        _pumping = true;
    }

    public void Stop()
    {
        _running = false;
        _pumping = false;
        _manager = null;
        _timer?.Stop();
    }

    void OnTick(object? sender, EventArgs e)
    {
        if (!_running)
            return;
        if (_manager?.Ticker is { SystemEnabled: false })
            return;

        var now = Environment.TickCount64;
        var delta = (now - _lastTicks) * (_manager?.SpeedModifier ?? 1);
        _lastTicks = now;
        if (delta > 0)
            _onDelta?.Invoke(delta);
    }

    public void Tick(double deltaMilliseconds)
    {
        if (!_running)
            return;
        _onDelta?.Invoke(deltaMilliseconds);
    }
}
