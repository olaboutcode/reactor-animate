using Reactor.Animate;
using MauiReactor;
using MotionRecipe = Reactor.Animate.Motion;

namespace Reactor.Animate.Internals;

/// <summary>
/// Wrapper behind <c>VisualNode.BindMotion</c>. Creates the player on Loaded and
/// disposes it on Unloaded. Playback does not start here; the caller uses
/// <c>Forward</c> or <c>ForwardAsync</c>.
/// </summary>
internal class MotionElement(MotionRecipe motion, Action<MotionPlayer>? onBind) : Component
{
    VisualElement? _element;
    MotionPlayer? _player;

    public override VisualNode Render()
        => Grid(element => _element = element, Children())
            .OnLoaded(Bind)
            .OnUnloaded(Unbind);

    void Bind()
    {
        var target = ResolveTarget(_element);
        if (target is null)
            return;

        _player?.Dispose();
        _player = MotionPlayer.Create(motion, [target]);
        onBind?.Invoke(_player);
    }

    void Unbind()
    {
        _player?.Dispose();
        _player = null;
    }

    static VisualElement? ResolveTarget(VisualElement? root)
    {
        var current = root;
        while (current is Layout layout)
        {
            VisualElement? only = null;
            var count = 0;
            foreach (var child in layout.Children)
            {
                if (child is not VisualElement visual)
                    continue;
                count++;
                if (count > 1)
                    return current;
                only = visual;
            }

            if (count != 1)
                return current;
            current = only;
        }

        return current;
    }
}
