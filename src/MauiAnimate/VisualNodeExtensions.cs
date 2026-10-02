using MauiReactor;

namespace Reactor.Animate;

/// <summary>
/// MauiReactor node extensions: host the app, tag a shared element, or bind a motion.
/// </summary>
public static class VisualNodeExtensions
{
    /// <summary>
    /// Hosts <paramref name="node"/> in a navigation page so
    /// <see cref="Animate.Page"/> can play shared-element flights.
    /// Call this once, around the root page.
    /// </summary>
    public static AnimatedHost AnimateHost(this VisualNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var host = new AnimatedHost
        {
            node
        };
        return host;
    }

    /// <summary>
    /// Tags <paramref name="node"/> so a later <see cref="Animate.Page"/> push
    /// can fly it to the view with the same <paramref name="tag"/>.
    /// Put gesture handlers on the control, then call this.
    /// </summary>
    /// <returns>A wrapper around <paramref name="node"/>. The first visual child flies.</returns>
    public static VisualNode Hero(this VisualNode node, string tag)
    {
        ArgumentNullException.ThrowIfNull(node);
        var hero = new HeroElement(tag)
        {
            node
        };
        return hero;
    }

    /// <summary>
    /// Binds <paramref name="motion"/> on Loaded. Does not start playback
    /// (even if <paramref name="onBind"/> is omitted). Call <c>Forward()</c> or
    /// <c>ForwardAsync()</c>.
    /// </summary>
    public static VisualNode BindMotion(
        this VisualNode node,
        Motion motion,
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
