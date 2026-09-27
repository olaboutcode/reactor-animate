namespace Sample.Components;

sealed class MotionColorPage : Component
{
    MauiControls.BoxView? _hsv;
    MauiControls.BoxView? _rgb;
    MotionPlayer? _hsvPlayer;
    MotionPlayer? _rgbPlayer;

    protected override void OnWillUnmount()
    {
        _hsvPlayer?.Dispose();
        _rgbPlayer?.Dispose();
        _hsvPlayer = null;
        _rgbPlayer = null;
        base.OnWillUnmount();
    }

    public override VisualNode Render()
        => ContentPage(
            Grid("auto,*", "*",
                NavigationBar
                    .BackNavigation("Home")
                    .GridRow(0),
                VStack(
                    Label("Color lerp")
                        .FontSize(28)
                        .HCenter(),
                    Label("HSV takes the short hue. RGB mixes channels.")
                        .FontSize(14)
                        .HCenter()
                        .TextColor(Colors.Gray),
                    HStack(
                        Swatch("HSV", b => _hsv = b, BindHsv),
                        Swatch("RGB", b => _rgb = b, BindRgb)
                    )
                    .Spacing(24)
                    .HCenter(),
                    Button("Play", Play)
                        .HCenter()
                )
                .Spacing(20)
                .Padding(24)
                .VCenter()
                .GridRow(1)
            )
        )
        .HideNavigationBar();

    static VisualNode Swatch(string title, Action<Microsoft.Maui.Controls.BoxView?> capture, Action onLoaded)
        => VStack(
            Label(title)
                .HCenter()
                .FontSize(16),
            BoxView(capture)
                .HeightRequest(96)
                .WidthRequest(96)
                .CornerRadius(16)
                .BackgroundColor(Colors.Red)
                .OnLoaded(onLoaded)
        )
        .Spacing(8);

    void BindHsv()
    {
        if (_hsv is null)
            return;
        _hsvPlayer?.Dispose();
        _hsvPlayer = Animate.Motion.Bind(
            Animate.Motion.Define(m => m
                .BackgroundColor(Colors.Red, Colors.Lime)
                .WithDuration(900)
                .WithEasing(Easing.Linear)
                .WithColorSpace(ColorSpace.Hsv)),
            _hsv);
    }

    void BindRgb()
    {
        if (_rgb is null)
            return;
        _rgbPlayer?.Dispose();
        _rgbPlayer = Animate.Motion.Bind(
            Animate.Motion.Define(m => m
                .BackgroundColor(Colors.Red, Colors.Lime)
                .WithDuration(900)
                .WithEasing(Easing.Linear)
                .WithColorSpace(ColorSpace.Rgb)),
            _rgb);
    }

    void Play()
    {
        _hsv?.SetValue(VisualElement.BackgroundColorProperty, Colors.Red);
        _rgb?.SetValue(VisualElement.BackgroundColorProperty, Colors.Red);
        _hsvPlayer.Forward();
        _rgbPlayer.Forward();
    }
}
