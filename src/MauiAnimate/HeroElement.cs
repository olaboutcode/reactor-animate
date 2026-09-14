using MauiReactor;

namespace Reactor.Animate;

class HeroElement : Component
{
    readonly string _tag;
    VisualElement? _element;

    public HeroElement(string tag) => _tag = tag;

    public override VisualNode Render()
        => Grid(element => _element = element, Children())
            .OnLoaded(Register)
            .OnUnloaded(Unregister);

    void Register()
    {
        if (_element is not null)
            HostContext.Current.RegisterHero(_tag, _element);
    }

    void Unregister()
    {
        if (_element is not null)
            HostContext.Current.UnregisterHero(_tag, _element);
    }
}

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
