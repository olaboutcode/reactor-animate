namespace Sample.Components;

sealed class GalleryItemProps
{
    public int Id { get; set; }

    public Color Color { get; set; } = Colors.CornflowerBlue;
}

sealed class GalleryPage : Component
{
    static readonly GalleryItemProps[] Items =
    [
        .. Enumerable.Range(0, 24).Select(id => new GalleryItemProps
        {
            Id = id,
            Color = Color.FromHsla(id / 24d, 0.55, 0.52),
        }),
    ];

    public override VisualNode Render()
        => ContentPage(
            Grid("auto, *", "*",
                HStack(
                    Button("Back", async () => await Animate.Page.PopAsync()),
                    Label("Gallery")
                        .FontSize(22)
                        .VCenter()
                )
                .Spacing(12)
                .Padding(16, 12)
                .GridRow(0),

                ScrollView(
                    Grid(
                        string.Join(", ", Enumerable.Repeat("auto", Rows)),
                        "*, *, *",
                        [.. Items.Select((item, i) => Tile(item, i / 3, i % 3))]
                    )
                    .ColumnSpacing(8)
                    .RowSpacing(8)
                    .Padding(16)
                )
                .GridRow(1)
            )
        )
        .HasNavigationBar(false);

    const int Rows = 8;

    static VisualNode Tile(GalleryItemProps item, int row, int column)
        => Grid(
            BoxView()
                .HeightRequest(96)
                .CornerRadius(16)
                .BackgroundColor(item.Color)
                .OnTapped(async () => await Open(item))
                .Hero($"tile-{item.Id}")
        )
        .GridRow(row)
        .GridColumn(column);

    static Task<MauiControls.Page> Open(GalleryItemProps item)
        => Animate.Page.PushAsync<GalleryDetailPage, GalleryItemProps>(t => t
            .Hero($"tile-{item.Id}", h => h.AnchorCenter())
            .WithEasing(Easing.CubicOut)
            .WithDuration(300),
            props =>
            {
                props.Id = item.Id;
                props.Color = item.Color;
            });
}
