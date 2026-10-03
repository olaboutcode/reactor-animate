namespace Sample.Components;

sealed class GalleryDetailPage : Component<GalleryDetailPage.EmptyState, GalleryItemProps>
{
    public sealed class EmptyState;

    public override VisualNode Render()
        => ContentPage(
            VStack(
                BoxView()
                    .HeightRequest(240)
                    .CornerRadius(28)
                    .BackgroundColor(Props.Color)
                    .Hero($"tile-{Props.Id}"),

                Label($"Item {Props.Id}")
                    .FontSize(28)
                    .HCenter()
                    .Margin(0, 24, 0, 0),

                Label("The cell you tapped is the cover.")
                    .FontSize(16)
                    .HCenter(),

                Button("Back", PageNavigation.Pop)
                    .HCenter()
                    .Margin(0, 16)
            )
        )
        .HideNavigationBar();
}
