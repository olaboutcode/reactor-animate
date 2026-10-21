namespace Sample.Components;

sealed class PushPopDetailPage : Component
{
    public override VisualNode Render()
        => ContentPage(
            VStack(
                BoxView()
                    .HeightRequest(240)
                    .CornerRadius(28)
                    .BackgroundColor(Colors.MediumPurple)
                    .Hero("pace"),

                Label("Back is the short flight")
                    .FontSize(28)
                    .HCenter()
                    .Margin(0, 24, 0, 0),

                Label("This cover arrived in 800 ms. Back plays the same tile in 200 ms.")
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
