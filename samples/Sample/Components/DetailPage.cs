namespace Sample.Components;

sealed class DetailPage : Component
{
    const double Cell = 128;

    public override VisualNode Render()
        => ContentPage(
            VStack(
                BoxView()
                    .HeightRequest(180)
                    .CornerRadius(24)
                    .BackgroundColor(Colors.MediumPurple)
                    .Hero("cover"),

                VStack(
                    HStack(
                        CellView("from_tl", Colors.CornflowerBlue),
                        CellView("from_center", Colors.LimeGreen),
                        CellView("from_tr", Colors.Teal)
                    ).Spacing(12),
                    HStack(
                        CellView("from_bl", Colors.Goldenrod),
                        CellView("spin_90", Colors.DeepSkyBlue),
                        CellView("from_br", Colors.HotPink)
                    ).Spacing(12)
                )
                .Spacing(12)
                .Padding(16)
                .HCenter(),

                BoxView()
                    .WidthRequest(140)
                    .HeightRequest(140)
                    .CornerRadius(16)
                    .BackgroundColor(Colors.Coral)
                    .HCenter()
                    .Hero("spin_180"),

                Button("Back", async () => await Animate.Page.PopAsync()).Hero("button")
            )
            .Spacing(16)
        )
        .HasNavigationBar(false)
        .OnBackButtonPressed(() => Animate.Page.PopAsync(), () => true);

    static VisualNode CellView(string tag, Color color)
        => BoxView()
            .WidthRequest(Cell)
            .HeightRequest(Cell)
            .CornerRadius(20)
            .BackgroundColor(color)
            .Hero(tag);
}
