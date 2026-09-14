namespace Sample.Components;

sealed class DetailPage : Component
{
    public override VisualNode Render()
        => ContentPage(
            Grid("180, *, auto", "*",
                BoxView()
                    .CornerRadius(24)
                    .BackgroundColor(Colors.MediumPurple)
                    .Hero("cover")
                    .GridRow(0),

                Grid("*, *", "*, *, *",
                    Cell("from_tl", Colors.CornflowerBlue, 0, 0),
                    Cell("from_center", Colors.LimeGreen, 0, 1),
                    Cell("from_tr", Colors.Teal, 0, 2),
                    Cell("from_bl", Colors.Goldenrod, 1, 0),
                    Cell("spin_90", Colors.DeepSkyBlue, 1, 1),
                    Cell("from_br", Colors.HotPink, 1, 2)
                )
                .Padding(16)
                .RowSpacing(12)
                .ColumnSpacing(12)
                .GridRow(1),

                VStack(
                    Grid(
                        BoxView()
                            .WidthRequest(140)
                            .HeightRequest(140)
                            .CornerRadius(16)
                            .BackgroundColor(Colors.Coral)
                            .Hero("spin_180")
                    )
                    .HCenter(),

                    Button("Back", async () => await Animate.Page.PopAsync()).Hero("button")
                )
                .Spacing(16)
                .Margin(16)
                .GridRow(2)
            )
        )
        .HasNavigationBar(false)
        .OnBackButtonPressed(() => Animate.Page.PopAsync(), () => true);

    static MauiReactor.Grid Cell(string tag, Color color, int row, int column)
        => Grid(
            BoxView()
                .CornerRadius(20)
                .BackgroundColor(color)
                .Hero(tag)
        )
        .GridRow(row)
        .GridColumn(column);
}
