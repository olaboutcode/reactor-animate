namespace Sample.Components;

sealed class DelayDetailPage : Component
{
    public override VisualNode Render()
        => ContentPage(
            VStack(
                BoxView()
                    .HeightRequest(240)
                    .CornerRadius(28)
                    .BackgroundColor(Colors.Teal)
                    .Hero("delay"),

                Label("Back starts at once")
                    .FontSize(28)
                    .HCenter()
                    .Margin(0, 24, 0, 0),

                Label("Push held the source pose for 700 ms, then flew this tile for 400 ms. Back does not wait.")
                    .FontSize(16)
                    .HorizontalTextAlignment(TextAlignment.Center)
                    .Padding(24, 8),

                Button("Back", PageNavigation.Pop)
                    .HCenter()
                    .Margin(0, 16)
            )
        )
        .HideNavigationBar();
}
