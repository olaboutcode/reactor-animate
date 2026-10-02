using MauiReactor;

namespace Reactor.Animate;

/// <summary>Marks a MauiReactor node as a shared element.</summary>
public static class HeroExtensions
{
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
}
