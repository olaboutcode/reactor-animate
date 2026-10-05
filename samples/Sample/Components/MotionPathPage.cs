using Microsoft.Maui.Controls.Shapes;

namespace Sample.Components;

sealed class MotionPathPage : Component
{
    MauiControls.BoxView? _bezier;
    MauiControls.BoxView? _arc;
    MotionPlayer? _bezierPlayer;
    MotionPlayer? _arcPlayer;

    protected override void OnWillUnmount()
    {
        _bezierPlayer?.Dispose();
        _arcPlayer?.Dispose();
        _bezierPlayer = null;
        _arcPlayer = null;
        base.OnWillUnmount();
    }

    public override VisualNode Render()
        => ContentPage(
            Grid("auto,*", "*",
                NavigationBar
                    .WithBackButton()
                    .MiddleView(Label("Path Motion").FontSize(FontSizing.Title))
                    .GridRow(0),

                VStack(
                    Label("Path")
                        .FontSize(28)
                        .HCenter(),
                    Label("Top: cubic Bézier. Bottom: clockwise arc.")
                        .FontSize(14)
                        .HCenter()
                        .TextColor(Colors.Gray),
                    Lane("Bézier", b => _bezier = b, BindBezier, Colors.OrangeRed, 200),
                    Lane("Arc", b => _arc = b, BindArc, Colors.CornflowerBlue, 160),
                    
                    HStack(
                        Button("Play", Play),
                        Button("Reset", () =>
                        {
                            _bezierPlayer?.Reset();
                            _arcPlayer?.Reset();
                            Reset(_bezier);
                            Reset(_arc);
                        })
                    ).HCenter()
                )
                .Spacing(16)
                .Padding(24)
                .VCenter()
                .GridRow(1)
            )
        )
        .HideNavigationBar();

    static VStack Lane(
        string title,
        Action<MauiControls.BoxView?> capture,
        Action onLoaded,
        Color color,
        double height)
        => VStack(
            Label(title)
                .FontSize(14)
                .TextColor(Colors.Gray),
            Grid(
                BoxView()
                    .BackgroundColor(Colors.Transparent)
                    .HeightRequest(height),
                BoxView(capture)
                    .HeightRequest(36)
                    .WidthRequest(36)
                    .CornerRadius(18)
                    .BackgroundColor(color)
                    .HStart()
                    .VCenter()
                    .OnLoaded(onLoaded)
            )
            .HeightRequest(height)
        )
        .Spacing(4);

    void BindBezier()
    {
        if (_bezier is null)
            return;
        _bezierPlayer?.Dispose();
        Reset(_bezier);
        _bezierPlayer = Animate.Motion.Bind(
            Animate.Motion.Define(m => m
                .Path(Bezier())
                .WithDuration(900)
                .WithEasing(Easing.SinInOut)),
            _bezier);
    }

    void BindArc()
    {
        if (_arc is null)
            return;
        _arcPlayer?.Dispose();
        Reset(_arc);
        _arcPlayer = Animate.Motion.Bind(
            Animate.Motion.Define(m => m
                .Path(Arc())
                .WithDuration(900)
                .WithEasing(Easing.SinInOut)),
            _arc);
    }

    void Play()
    {
        Reset(_bezier);
        Reset(_arc);
        _bezierPlayer.Forward();
        _arcPlayer.Forward();
    }

    static void Reset(Microsoft.Maui.Controls.BoxView? orb)
    {
        if (orb is null)
            return;
        orb.TranslationX = 0;
        orb.TranslationY = 0;
    }

    static PathGeometry Bezier()
    {
        var geometry = new PathGeometry();
        var figure = new PathFigure { StartPoint = new Point(0, 0) };
        figure.Segments.Add(new BezierSegment
        {
            Point1 = new Point(70, -90),
            Point2 = new Point(170, 90),
            Point3 = new Point(260, 0),
        });
        geometry.Figures.Add(figure);
        return geometry;
    }

    static PathGeometry Arc()
    {
        var geometry = new PathGeometry();
        var figure = new PathFigure { StartPoint = new Point(0, 0) };
        figure.Segments.Add(new ArcSegment
        {
            Point = new Point(240, 0),
            Size = new Size(120, 90),
            SweepDirection = SweepDirection.Clockwise,
            IsLargeArc = false,
        });
        geometry.Figures.Add(figure);
        return geometry;
    }
}
