using Reactor.Animate;

namespace Sample.Components;

sealed class App : Component
{
    public override VisualNode Render()
        => new HomePage().AnimateHost();
}
