namespace Sample.Components;

sealed class HomePage : Component
{
    const double Cell = 72;

    public override VisualNode Render()
        => ContentPage(
            VStack(
                Label("Reactor.Animate")
                    .FontSize(28)
                    .HCenter(),

                VStack(
                    HStack(
                        CellView("cover", Colors.OrangeRed),
                        CellView("from_tl", Colors.CornflowerBlue),
                        CellView("from_tr", Colors.Teal)
                    ).Spacing(12),
                    HStack(
                        CellView("from_bl", Colors.Goldenrod),
                        CellView("from_center", Colors.LimeGreen),
                        CellView("from_br", Colors.HotPink)
                    ).Spacing(12),
                    HStack(
                        CellView("spin_90", Colors.DeepSkyBlue),
                        CellView("spin_180", Colors.Coral)
                    ).Spacing(12)
                )
                .Spacing(12)
                .HCenter(),

                Button("Open", async () => await Open()).Hero("button")
            )
            .Spacing(20)
            .Padding(24)
            .VCenter()
        )
        .HasNavigationBar(false);

    static VisualNode CellView(string tag, Color color)
        => BoxView()
            .WidthRequest(Cell)
            .HeightRequest(Cell)
            .CornerRadius(12)
            .BackgroundColor(color)
            .OnTapped(async () => await Open())
            .Hero(tag);

    static Task<MauiControls.Page> Open()
        => Animate.Page.PushAsync<DetailPage>(t => t
            .Hero("cover", h => h.AnchorCenter())
            .Hero("from_tl", h => h.AnchorTopLeft())
            .Hero("from_tr", h => h.AnchorTopRight())
            .Hero("from_bl", h => h.AnchorBottomLeft())
            .Hero("from_br", h => h.AnchorBottomRight())
            .Hero("from_center", h => h.AnchorCenter())
            .Hero("spin_90", h => h.AnchorCenter().Rotate(90))
            .Hero("spin_180", h => h.AnchorCenter().Rotate(180))
            .Hero("button", h => h.AnchorCenter())
            .WithEasing(Easing.CubicOut));
}
