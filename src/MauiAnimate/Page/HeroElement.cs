using MauiReactor;

namespace Reactor.Animate.Page;

internal class HeroElement(string tag) : Component
{
    readonly string _tag = tag;
    VisualElement? _element;
    VisualElement? _target;

    public override VisualNode Render()
        => Grid(element => _element = element, Children())
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


