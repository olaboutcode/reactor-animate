using Reactor.Animate;

namespace Reactor.Animate.Internals;

/// <summary>Where a <see cref="MotionPlayer"/> is in its run.</summary>
internal enum MotionPlaybackStatus
{
    /// <summary>No run, or a reverse or <see cref="MotionPlayer.Reset"/> returned to the start.</summary>
    Dismissed,

    /// <summary>Playing toward the end.</summary>
    Forward,

    /// <summary>Playing toward the start.</summary>
    Reverse,

    /// <summary>Stopped in the middle by pause, cancel, or a seek inside the span.</summary>
    Paused,

    /// <summary>A forward run reached the end, including the last repeat.</summary>
    Completed,
}
