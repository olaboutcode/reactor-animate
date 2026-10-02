using MauiPage = Microsoft.Maui.Controls.Page;

namespace Reactor.Animate;

/// <summary>
/// Payload for <see cref="Animate.Page.HeroStarted"/>, <see cref="Animate.Page.HeroInFlight"/>,
/// and <see cref="Animate.Page.HeroEnded"/>.
/// </summary>
public sealed class HeroTransitionEventArgs : EventArgs
{
    readonly HeroTransitionKind _kind;
    readonly List<Action<double>> _listeners = [];

    internal HeroTransitionEventArgs(HeroTransitionKind kind, Transition transition, MauiPage page)
    {
        ArgumentNullException.ThrowIfNull(transition);
        ArgumentNullException.ThrowIfNull(page);
        _kind = kind;
        Transition = transition;
        Page = page;
    }

    /// <summary>Shared-element flight while a page is pushed.</summary>
    public bool IsPushTransition() => _kind == HeroTransitionKind.Push;

    /// <summary>Reverse flight while a page is popped.</summary>
    public bool IsPopTransition() => _kind == HeroTransitionKind.Pop;

    /// <summary>The transition playing for this flight.</summary>
    public Transition Transition { get; }

    /// <summary>
    /// Destination page on push; the page being revealed on pop.
    /// </summary>
    public MauiPage Page { get; }

    /// <summary>Tags on <see cref="Transition"/>.</summary>
    public IReadOnlyList<string> Tags => Transition.Tags;

    /// <summary>
    /// 0–1 along the clip, using the same easing as the flight
    /// (<see cref="Transition.Easing"/>). 0 at
    /// <see cref="Animate.Page.HeroStarted"/>, 1 at
    /// <see cref="Animate.Page.HeroEnded"/>.
    /// </summary>
    public double Progress { get; private set; }

    /// <summary>
    /// Invokes <paramref name="callback"/> with <see cref="Progress"/> now and
    /// on each tick of this flight.
    /// </summary>
    public void At(Action<double> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _listeners.Add(callback);
        Invoke(callback, Progress);
    }

    internal void ReportProgress(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return;

        var progress = Math.Clamp(value, 0, 1);
        if (progress <= Progress)
            return;

        Progress = progress;
        foreach (var callback in _listeners.ToArray())
            Invoke(callback, Progress);
    }

    static void Invoke(Action<double> callback, double progress)
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
