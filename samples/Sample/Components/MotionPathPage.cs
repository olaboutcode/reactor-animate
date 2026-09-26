using Microsoft.Maui.Controls.Shapes;

namespace Sample.Components;

sealed class MotionPathPage : Component
{
    Microsoft.Maui.Controls.BoxView? _orb;
    MotionPlayer? _player;

    protected override void OnWillUnmount()
    {
        _player?.Dispose();
        _player = null;
        base.OnWillUnmount();
    }

    public override VisualNode Render()
        => ContentPage(
            Grid("auto,*", "*",
                NavigationBar
                    .BackNavigation("Home")
                    .GridRow(0),
                VStack(
                    Label("Path")
                        .FontSize(28)
                        .HCenter(),
                    Label("The orb follows a cubic Bézier.")
                        .FontSize(14)
                        .HCenter()
                        .TextColor(Colors.Gray),
                    Grid(
                        BoxView()
                            .BackgroundColor(Colors.Transparent)
                            .HeightRequest(200),
                        BoxView(b => _orb = b)
                            .HeightRequest(36)
                            .WidthRequest(36)
                            .CornerRadius(18)
                            .BackgroundColor(Colors.OrangeRed)
                            .HStart()
                            .VCenter()
                            .OnLoaded(Bind)
                    )
                    .HeightRequest(200),
                    Button("Play", () => _ = Play())
                        .HCenter()
                )
                .Spacing(16)
                .Padding(24)
                .VCenter()
                .GridRow(1)
            )
        )
        .HideNavigationBar();

    void Bind()
    {
        if (_orb is null)
            return;
        _player?.Dispose();
        _orb.TranslationX = 0;
        _orb.TranslationY = 0;
        _player = Animate.Motion.Bind(
            Animate.Motion.Define(m => m
                .Path(Curve())
                .WithDuration(900)
                .WithEasing(Easing.SinInOut)),
            _orb);
    }

    async Task Play()
    {
        if (_player is null)
            return;
        _orb!.TranslationX = 0;
        _orb.TranslationY = 0;
        try
        {
            await _player.ForwardAsync();
        }
        catch (OperationCanceledException)
        {
        }
    }

    static PathGeometry Curve()
    {
        var geometry = new PathGeometry();
        var figure = new PathFigure { StartPoint = new Point(0, 0) };
        figure.Segments.Add(new BezierSegment
        {
            Point1 = new Point(70, -110),
            Point2 = new Point(170, 110),
            Point3 = new Point(260, 0),
        });
        geometry.Figures.Add(figure);
        return geometry;
    }
}
