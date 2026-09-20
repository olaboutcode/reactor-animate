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
        .. Enumerable.Range(0, 36).Select(id => new GalleryItemProps
        {
            Id = id,
            Color = Color.FromHsla(id / 36d, 0.55, 0.52),
        }),
    ];

    public override VisualNode Render()
        => ContentPage(
            Grid("auto,*", "*",
                Grid("*","auto,*",
                    Button("Back")
                        .BackgroundColor(Colors.Transparent)
                        .OnClicked(async () => await Animate.Page.PopAsync())
                        .GridColumn(0),
                        
                    Label("Gallery")
                        .FontSize(16)
                        .FontAttributes(FontAttributes.Bold)
                        .TextTransform(TextTransform.Uppercase)
                        .TextColor(Colors.Black)
                        .Center()
                        .GridColumn(1)
                )
                .HeightRequest(54)
                .BackgroundColor(Colors.Orange)
                .GridRow(0),

                CollectionView()
                .ItemsLayout(
                    new VerticalGridItemsLayout(3)
                        .HorizontalItemSpacing(8)
                        .VerticalItemSpacing(8))
                .ItemsSource(Items, Tile)
                .Margin(16)
                .GridRow(1)
            )
        )
        .HasNavigationBar(false)
        .OnBackButtonPressed(async () => await Animate.Page.PopAsync());

    static VisualNode Tile(GalleryItemProps item)
        => BoxView()
            .HeightRequest(112)
            .CornerRadius(16)
            .BackgroundColor(item.Color)
            .OnTapped(async () => await Open(item))
            .Hero($"tile-{item.Id}");

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
