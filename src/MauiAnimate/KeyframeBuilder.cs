namespace Reactor.Animate;

/// <summary>
/// Stop list for one <see cref="Motion"/> track. Offsets are fractions of that
/// motion, from 0 to 1.
/// </summary>
/// <typeparam name="T">Value written by the track, such as <see cref="double"/> or <see cref="Color"/>.</typeparam>
public sealed class KeyframeBuilder<T>
{
    readonly List<MotionKeyframe> _frames = [];

    /// <summary>
    /// Appends a stop. <paramref name="offset"/> is clamped to 0–1.
    /// <paramref name="easing"/> is the curve into this stop; omit it to use the motion easing.
    /// </summary>
    /// <returns>This builder, so calls can chain.</returns>
    public KeyframeBuilder<T> At(double offset, T value, Easing? easing = null)
    {
        if (double.IsNaN(offset) || double.IsInfinity(offset))
            offset = 0;
        offset = Math.Clamp(offset, 0, 1);
        if (_frames.Count > 0 && offset < _frames[^1].Offset)
        {
            System.Diagnostics.Debug.Assert(false, "Keyframe offsets must be non-decreasing.");
            offset = _frames[^1].Offset;
        }

        _frames.Add(new MotionKeyframe(offset, value!, easing));
        return this;
    }

    internal IReadOnlyList<MotionKeyframe> Build() => _frames;
}
