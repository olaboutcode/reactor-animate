using Reactor.Animate;
using MauiReactor;
using MotionRecipe = Reactor.Animate.Motion;

namespace Reactor.Animate.Internals;

// VisualNode.BindMotion wrapper. Creates the player on Loaded and disposes it on Unloaded.
// Does not start playback.
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
