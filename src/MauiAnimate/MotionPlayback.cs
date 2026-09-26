using MotionRecipe = Reactor.Animate.Animation.Motion;

namespace Reactor.Animate;

public enum MotionPlaybackStatus
{
    Dismissed,
    Forward,
    Reverse,
    Paused,
    Completed,
}

public sealed class MotionPlaybackEventArgs : EventArgs
{
    readonly List<Action<double>> _listeners = [];

    internal MotionPlaybackEventArgs(
        MotionRecipe motion,
        IReadOnlyList<VisualElement> targets,
        MotionPlaybackStatus status,
        double progress)
    {
        Motion = motion;
        Targets = targets;
        Status = status;
        Progress = progress;
    }

    public MotionRecipe Motion { get; }

    public IReadOnlyList<VisualElement> Targets { get; }

    public MotionPlaybackStatus Status { get; internal set; }

    /// <summary>
    /// Eased 0–1 of linear u. Increases on forward, decreases on reverse.
    /// Unlike <see cref="HeroTransitionEventArgs.Progress"/>, ticks are not monotonic.
    /// </summary>
    public double Progress { get; private set; }

    /// <summary>
    /// Invokes <paramref name="callback"/> with <see cref="Progress"/> now and on
    /// every later tick of this run, including decreases.
    /// </summary>
    public void At(Action<double> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _listeners.Add(callback);
        Invoke(callback, Progress);
    }

    internal void Report(double progress)
    {
        Progress = progress;
        foreach (var callback in _listeners.ToArray())
            Invoke(callback, progress);
    }

    internal static void Invoke(Action<double> callback, double progress)
    {
        try
        {
            callback(progress);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
}
