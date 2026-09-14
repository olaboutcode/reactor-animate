using MauiReactor;
using Reactor.Animate.Page;

namespace Reactor.Animate;

public static class HeroExtensions
{
    public static VisualNode Hero(this VisualNode node, string tag)
    {
        ArgumentNullException.ThrowIfNull(node);
        var hero = new HeroElement(tag);
        hero.Add(node);
        return hero;
    }
}
