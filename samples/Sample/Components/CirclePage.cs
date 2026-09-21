namespace Sample.Components;

sealed class CirclePage : Component
{
    public override VisualNode Render()
        => ContentPage(
            BoxView()
                .WidthRequest(88)
                .HeightRequest(88)
                .CornerRadius(44)
                .BackgroundColor(Colors.OrangeRed)
                .HCenter()
                .VCenter()
                .OnTapped(Open)
                .Hero("orb")
        )
        .HideNavigationBar();

    static Task Open()
        => Animate.Page.PushAsync<CircleDetailPage>(t => t
            .Hero("orb", h => h.AnchorCenter())
            .WithDuration(300)
            .WithEasing(Easing.CubicOut));
}
