namespace Sample.Components;

sealed class PushPopPage : Component
{
    public override VisualNode Render()
        => ContentPage(
            Grid("auto,*", "*",
                NavigationBar
                    .BackNavigation("Home")
                    .GridRow(0),

                VStack(
                    BoxView()
                        .WidthRequest(88)
                        .HeightRequest(88)
                        .CornerRadius(20)
                        .BackgroundColor(Colors.MediumPurple)
                        .HCenter()
                        .OnTapped(Open)
                        .Hero("pace"),

                    Label("Tap the tile")
                        .FontSize(16)
                        .HCenter(),

                    Label("Push takes 800 ms and settles with CubicOut. Pop takes 200 ms and finishes with CubicIn. The tile is the same flight both ways.")
                        .FontSize(16)
                        .HorizontalTextAlignment(TextAlignment.Center)
                )
                .Spacing(16)
                .Padding(24)
                .VCenter()
                .GridRow(1)
            )
        )
        .HideNavigationBar();

    static Task Open()
        => Animate.Page.PushAsync<PushPopDetailPage>(t => t
            .Hero("pace", h => h.AnchorCenter())
            .WithDuration(800)
            .WithPopDuration(200)
            .WithEasing(Easing.CubicOut)
            .WithPopEasing(Easing.CubicIn));
}
