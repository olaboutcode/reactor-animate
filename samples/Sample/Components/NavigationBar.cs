namespace Sample.Components;

public partial class NavigationBar: Component
{
    [Prop]private VisualNode? _leftView;
    [Prop]private VisualNode? _middleView;
    [Prop]private VisualNode? _rightView;

    public override VisualNode Render()
    {
        return Grid(
            _leftView is null
            ? null
            : ContentView(_leftView)
                .ZIndex(10)
                .VCenter()
                .HStart(),

            _leftView is null
            ? null
            : ContentView(_middleView)
                .ZIndex(10)
                .VCenter()
                .HCenter(),

            _leftView is null
            ? null
            : ContentView(_rightView)
                .ZIndex(10)
                .VCenter()
                .HEnd()
        )
        .MinimumHeightRequest(TouchTarget.Min)
        .HFill()
        .BackgroundColor(Colors.Transparent)
        .Padding(Spacing.Medium, Spacing.XSmall);
    }
}
