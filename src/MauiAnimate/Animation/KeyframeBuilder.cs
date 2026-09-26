namespace Reactor.Animate.Animation;

public sealed class KeyframeBuilder<T>
{
    readonly List<MotionKeyframe> _frames = [];

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
