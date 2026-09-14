using MauiReactor;

namespace Reactor.Animate;

class HeroElement : Component
{
    readonly string _tag;
    VisualElement? _element;
    VisualElement? _target;

    public HeroElement(string tag) => _tag = tag;

    public override VisualNode Render()
        => Grid(element => _element = element, Children())
            .HCenter()
            .OnLoaded(Register)
            .OnUnloaded(Unregister);

    void Register()
    {
        _target = Target();
        if (_target is not null)
            HostContext.Current.RegisterHero(_tag, _target);
    }

    void Unregister()
    {
        if (_target is not null)
            HostContext.Current.UnregisterHero(_tag, _target);
        _target = null;
    }

    VisualElement? Target()
    {
        if (_element is Layout layout)
        {
            foreach (var child in layout.Children)
            {
                if (child is VisualElement visual)
                    return visual;
            }
        }

        return _element;
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
