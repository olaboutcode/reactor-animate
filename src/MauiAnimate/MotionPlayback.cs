using MotionRecipe = Reactor.Animate.Motion;

namespace Reactor.Animate;

/// <summary>Payload for <see cref="MotionPlayer"/> playback events.</summary>
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

    /// <summary>The recipe being played.</summary>
    public MotionRecipe Motion { get; }

    /// <summary>Views bound to this run.</summary>
    public IReadOnlyList<VisualElement> Targets { get; }

    internal MotionPlaybackStatus Status { get; set; }

    /// <summary>No run, or a reverse or reset returned to the start.</summary>
    public bool IsDismissed() => Status == MotionPlaybackStatus.Dismissed;

    /// <summary>Playing toward the end.</summary>
    public bool IsForward() => Status == MotionPlaybackStatus.Forward;

    /// <summary>Playing toward the start.</summary>
    public bool IsReverse() => Status == MotionPlaybackStatus.Reverse;

    /// <summary>Stopped in the middle by pause, cancel, or a seek inside the span.</summary>
    public bool IsPaused() => Status == MotionPlaybackStatus.Paused;

    /// <summary>A forward run reached the end, including the last repeat.</summary>
    public bool IsCompleted() => Status == MotionPlaybackStatus.Completed;

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
