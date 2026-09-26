namespace Sample.Components;

sealed class PhotoItemProps
{
    public int Id { get; set; }

    public string Source { get; set; } = "";

    public string Title { get; set; } = "";
}

sealed class PhotoPage : Component
{
    static readonly PhotoItemProps[] Photos =
    [
        new() { Id = 0, Source = "photo_sunset", Title = "Sunset" },
        new() { Id = 1, Source = "photo_sea", Title = "Sea" },
        new() { Id = 2, Source = "photo_forest", Title = "Forest" },
        new() { Id = 3, Source = "photo_dunes", Title = "Dunes" },
        new() { Id = 4, Source = "photo_night", Title = "Night" },
        new() { Id = 5, Source = "photo_city", Title = "City" },
    ];

    public override VisualNode Render()
        => ContentPage(
            Grid("auto,*", "*",
                NavigationBar
                    .BackNavigation("Home")
                    .GridRow(0),
                CollectionView()
                    .ItemsLayout(
                        new VerticalGridItemsLayout(3)
                            .HorizontalItemSpacing(8)
                            .VerticalItemSpacing(8))
                    .ItemsSource(Photos, Tile)
                    .Margin(16)
                    .GridRow(1)
            )
        )
        .HideNavigationBar();

    static VisualNode Tile(PhotoItemProps item)
        => Image(item.Source)
            .Aspect(Aspect.AspectFill)
            .HeightRequest(112)
            .OnTapped(async () => await Open(item))
            .Hero($"photo-{item.Id}");

    static Task<MauiControls.Page> Open(PhotoItemProps item)
        => Animate.Page.PushAsync<PhotoDetailPage, PhotoItemProps>(t => t
            .Hero($"photo-{item.Id}", h => h.AnchorCenter())
            .WithEasing(Easing.CubicOut)
            .WithDuration(300),
            props =>
            {
                props.Id = item.Id;
                props.Source = item.Source;
                props.Title = item.Title;
            });
}
