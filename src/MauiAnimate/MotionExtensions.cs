using MauiReactor;
using MotionRecipe = Reactor.Animate.Animation.Motion;

namespace Reactor.Animate;

public static class MotionExtensions
{
    /// <summary>
    /// Binds <paramref name="motion"/> on Loaded. Does not start playback
    /// (even if <paramref name="onBind"/> is omitted). Call Play / ForwardAsync.
    /// </summary>
    public static VisualNode BindMotion(this VisualNode node, MotionRecipe motion, Action<MotionPlayer>? onBind = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(motion);
        return new Motion.MotionElement(motion, onBind)
        {
            node
        };
    }
}
