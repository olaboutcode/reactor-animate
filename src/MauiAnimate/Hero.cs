using MauiReactor;

namespace Reactor.Animate;

/// <summary>
/// Marks any visual subtree as a shared element. Matching is by tag across pages.
/// </summary>
public class Hero : Component
{
    readonly string _tag;
    VisualElement? _element;

    public Hero(string tag) => _tag = tag;

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
