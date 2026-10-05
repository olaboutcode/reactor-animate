namespace Sample.Components;

sealed class StaggerGridPage : Component
{
    readonly MauiControls.BoxView?[] _tiles = new MauiControls.BoxView?[12];
    MotionPlayer? _player;

    public override VisualNode Render()
        => ContentPage(
            Grid("auto,*", "*",
                NavigationBar
                    .WithBackButton()
                    .MiddleView(Label("Stagger Grid").FontSize(FontSizing.Title))
                    .GridRow(0),

                VStack(
                    Grid(
                        "auto, auto, auto, auto",
                        "*, *, *",
                        [.. Tiles()]
                    )
                    .ColumnSpacing(8)
                    .RowSpacing(8),
                    Button("Play stagger", Play)
                )
                .Spacing(16)
                .Padding(16)
                .GridRow(1)
            )
        )
        .HideNavigationBar();

    IEnumerable<VisualNode> Tiles()
    {
        for (var i = 0; i < _tiles.Length; i++)
        {
            var index = i;
            yield return BoxView(box => _tiles[index] = box)
                .HeightRequest(72)
                .CornerRadius(16)
                .BackgroundColor(Color.FromHsla(index / 36d, 0.55, 0.52))
                .GridRow(index / 3)
                .GridColumn(index % 3);
        }
    }

    void Play()
    {
        var views = _tiles.OfType<Microsoft.Maui.Controls.BoxView>().ToArray();
        if (views.Length == 0)
            return;

        _player?.Dispose();
        _player = Animate.Motion.Play(
            m => m
                .FadeIn()
                .Scale(0.9, 1)
                .Stagger(40, StaggerFrom.Start, (3, 4))
                .WithDuration(280),
            views);
    }
}
