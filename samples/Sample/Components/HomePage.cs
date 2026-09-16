namespace Sample.Components;

sealed class HomePage : Component
{
    public override VisualNode Render()
        => ContentPage(
            ScrollView(
            VStack(
                Label("Reactor.Animate")
                    .FontSize(28)
                    .HCenter(),

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
                .RowSpacing(12),

                Button("Open", async () => await Open()).Hero("button"),

                Label("Page recipes")
                    .FontSize(16)
                    .HCenter()
                    .Margin(0, 8, 0, 0),

                Grid("auto, auto, auto", "*, *",
                    Recipe("Fade", Colors.MediumPurple, 0, 0,
                        "Incoming page fades in. Pop fades the home page in.",
                        t => t.Fade()),
                    Recipe("Scale", Colors.CadetBlue, 0, 1,
                        "Incoming page scales from 0.92 to 1.",
                        t => t.Scale()),
                    Recipe("Slide →", Colors.Tomato, 1, 0,
                        "Slides in from the right. Pop slides in from the left.",
                        t => t.SlideFrom(SlideEdge.Right)),
                    Recipe("Slide ↑", Colors.DarkCyan, 1, 1,
                        "Slides in from the bottom. Pop slides in from the top.",
                        t => t.SlideFrom(SlideEdge.Down)),
                    Recipe("Fade + Scale", Colors.SlateBlue, 2, 0,
                        "Fade and scale on the incoming page.",
                        t => t.Fade().Scale(0.92)),
                    Recipe("Gallery", Colors.SeaGreen, 2, 1,
                        "Slide into the gallery.",
                        t => t.SlideFrom(SlideEdge.Right),
                        gallery: true)
                )
                .ColumnSpacing(8)
                .RowSpacing(8)
            )
            .Spacing(20)
            .Padding(24)
            )
        )
        .HasNavigationBar(false);

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

    static VisualNode Recipe(
        string title,
        Color accent,
        int row,
        int column,
        string body,
        Func<Transition, Transition> recipe,
        bool gallery = false)
        => Button(title, async () =>
            {
                if (gallery)
                {
                    await Animate.Page.PushAsync<GalleryPage>(recipe);
                    return;
                }

                await Animate.Page.PushAsync<RecipePage, RecipePageProps>(
                    recipe,
                    props =>
                    {
                        props.Title = title;
                        props.Body = body;
                        props.Accent = accent;
                    });
            })
            .GridRow(row)
            .GridColumn(column);

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
            .WithEasing(Easing.CubicInOut));
}
