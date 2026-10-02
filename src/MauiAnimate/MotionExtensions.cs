using MauiReactor;
using MotionRecipe = Reactor.Animate.Motion;

namespace Reactor.Animate;

/// <summary>Binds a <see cref="Motion"/> recipe to a MauiReactor node.</summary>
public static class MotionExtensions
{
    /// <summary>
    /// Binds <paramref name="motion"/> on Loaded. Does not start playback
    /// (even if <paramref name="onBind"/> is omitted). Call <c>Forward()</c> or
    /// <c>ForwardAsync()</c>.
    /// </summary>
    public static VisualNode BindMotion(
        this VisualNode node,
        MotionRecipe motion,
        Action<MotionPlayer>? onBind = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(motion);
        return new MotionElement(motion, onBind)
        {
            node
        };
    }
}
