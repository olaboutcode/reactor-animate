using Reactor.Animate;
using Microsoft.Maui.Animations;

namespace Reactor.Animate.Internals;

// Frame pump shared by MotionPlayer and FlipClip. Reports delta milliseconds.
// Manual clocks do not subscribe to the platform; tests call Tick.
internal sealed class MotionClock
{
    readonly bool _manual;
    Action<double>? _onDelta;
    IAnimationManager? _manager;
    IDispatcherTimer? _timer;
    bool _running;
    bool _loggedEnergySaver;

    public MotionClock(bool manual = false)
    {
        _manual = manual;
    }

    public void Connect(Action<double> onDelta)
        => _onDelta = onDelta ?? throw new ArgumentNullException(nameof(onDelta));

    public bool IsRunning => _running;

    public bool IsPumping => _manual || _timer is not null;

    public void Start(IEnumerable<VisualElement> targets)
    {
        if (_running)
            return;
        _running = true;
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

        if (_manager?.Ticker is { SystemEnabled: false } && !_loggedEnergySaver)
        {
            _loggedEnergySaver = true;
            System.Diagnostics.Debug.WriteLine("MotionClock: animation ticker disabled; freezing.");
            return;
        }

        if (Application.Current?.Dispatcher is not { } dispatcher)
            return;

        var last = Environment.TickCount64;
        _timer = dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.Tick += (_, _) =>
        {
            if (_manager?.Ticker is { SystemEnabled: false })
                return;
            var now = Environment.TickCount64;
            var delta = (now - last) * (_manager?.SpeedModifier ?? 1);
            last = now;
            if (delta > 0)
                _onDelta?.Invoke(delta);
        };
        _timer.Start();
    }

    public void Stop()
    {
        _running = false;
        _manager = null;
        if (_timer is not null)
        {
            _timer.Stop();
            _timer = null;
        }
    }

    public void Tick(double deltaMilliseconds)
    {
        if (!_running)
            return;
        _onDelta?.Invoke(deltaMilliseconds);
    }
}
