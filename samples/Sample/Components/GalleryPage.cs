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
            Grid("auto,*", "*",
                new NavigationBar()
                    .LeftView(CustomButton
                        .BackNavButton()
                        .OnTapped(async () => await Animate.Page.PopAsync()))
                    .MiddleView(Label("Gallery").FontSize(FontSizing.Title))
                    .GridRow(0),

                CollectionView()
                    .ItemsLayout(
                        new VerticalGridItemsLayout(3)
                            .HorizontalItemSpacing(8)
                            .VerticalItemSpacing(8))
                    .ItemsSource(Items, Tile)
                    .VerticalScrollBarVisibility(ScrollBarVisibility.Never)
                    .Margin(Spacing.Medium, Spacing.XSmall)
                    .GridRow(1)
            )
        )
        .HideNavigationBar();

    static VisualNode Tile(GalleryItemProps item)
        => BoxView()
            .HeightRequest(112)
            .CornerRadius(Radius.Large)
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
