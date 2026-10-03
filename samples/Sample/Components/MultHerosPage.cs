namespace Sample.Components;

sealed class MultiHerosPage : Component
{
    public override VisualNode Render()
        => ContentPage(
            Grid("auto,*, auto", "*",
                NavigationBar
                    .BackNavigation("Home")
                    .GridRow(0),
                ScrollView(
                    Grid("auto, auto, auto", "*, *, *",
                        Cell("cover", Colors.OrangeRed, 0, 0),
                        Cell("from_tl", Colors.CornflowerBlue, 0, 1),
                        Cell("from_tr", Colors.Teal, 0, 2),
                        Cell("from_bl", Colors.Goldenrod, 1, 0),
                        Cell("from_center", Colors.LimeGreen, 1, 1),
                        Cell("from_br", Colors.HotPink, 1, 2),
                        Cell("spin_90", Colors.DeepSkyBlue, 2, 0),
                        Cell("spin_180", Colors.Coral, 2, 1)
                    )
                    .ColumnSpacing(12)
                    .RowSpacing(12)
                )
                .GridRow(1),

                Button("Play Heroes", () => PageNavigation.Run(Open))
                    .Margin(16)
                    .GridRow(2)
            )
        ).HideNavigationBar();

    static MauiReactor.Grid Cell(string tag, Color color, int row, int column)
        => Grid(
            BoxView()
                .HeightRequest(72)
                .CornerRadius(12)
                .BackgroundColor(color)
                .OnTapped(async () => await Open())
                .Hero(tag)
        )
        .GridRow(row)
        .GridColumn(column);

    static Task<MauiControls.Page> Open()
        => Animate.Page.PushAsync<MultiHerosDetailsPage>(t => t
            .Hero("cover", h => h.AnchorCenter())
            .Hero("from_tl", h => h.AnchorTopLeft())
            .Hero("from_tr", h => h.AnchorTopRight())
            .Hero("from_bl", h => h.AnchorBottomLeft())
            .Hero("from_br", h => h.AnchorBottomRight())
            .Hero("from_center", h => h.AnchorCenter())
            .Hero("spin_90", h => h.AnchorCenter().Rotate(90))
            .Hero("spin_180", h => h.AnchorCenter().Rotate(180))
            .Hero("button", h => h.AnchorCenter())
            .WithEasing(Easing.CubicInOut));
}