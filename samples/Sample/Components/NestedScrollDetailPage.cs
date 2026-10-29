namespace Sample.Components;

sealed class NestedScrollDetailPage : Component<NestedScrollDetailPage.EmptyState, NestedCardProps>
{
    public sealed class EmptyState;

    public override VisualNode Render()
        => ContentPage(
            Grid("auto,*", "*",
                NavigationBar
                    .BackNavigation(Props.Name)
                    .GridRow(0),

                ScrollView(
                    VStack(
                        BoxView()
                            .HeightRequest(180)
                            .CornerRadius(24)
                            .BackgroundColor(Props.Color)
                            .Hero(Props.Name),

                        Label(Props.Name)
                            .FontSize(28),

                        Label("This card sat in a horizontal row inside the page scroll.")
                            .FontSize(16)
                    )
                    .Spacing(16)
                    .Padding(24)
                )
                .GridRow(1)
            )
        )
        .HideNavigationBar()
        .SafeAreaEdges(SafeAreaEdges.All);
}
