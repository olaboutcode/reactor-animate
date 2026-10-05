namespace Sample.Components;

sealed class MotionScrubPageState
{
    public double T { get; set; }
}

sealed class MotionScrubPage : Component<MotionScrubPageState>
{
    Microsoft.Maui.Controls.BoxView? _box;
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
                new NavigationBar()
                    .LeftView(
                        new AnimateButton()
                        .Icon(HeroIcons.ArrowLeft)
                        .OnClicked(async () => await Animate.Page.PopAsync())
                    )
                    .MiddleView(Label("Motion Scrub").FontSize(FontSizing.Title))
                    .GridRow(0),

                VStack(
                    Label("Scrub")
                        .FontSize(28)
                        .HCenter(),
                    Label("Drag the slider. SeekFraction is eased progress.")
                        .FontSize(14)
                        .HCenter()
                        .TextColor(Colors.Gray),
                    BoxView(b => _box = b)
                        .HeightRequest(96)
                        .WidthRequest(96)
                        .CornerRadius(16)
                        .BackgroundColor(Colors.OrangeRed)
                        .HCenter()
                        .OnLoaded(Bind),
                    Slider()
                        .Minimum(0)
                        .Maximum(1)
                        .Value(State.T)
                        .OnValueChanged(OnScrub),
                    Label($"t = {State.T:0.00}")
                        .HCenter()
                        .TextColor(Colors.Gray),
                    HStack(
                        Button("Play", () => _ = Play()),
                        Button("Reset", Reset)
                    )
                    .Spacing(8)
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
        if (_box is null)
            return;
        _player?.Dispose();
        _box.Opacity = 0;
        _box.ScaleX = 0.8;
        _box.ScaleY = 0.8;
        _box.Rotation = 0;
        _player = Animate.Motion.Bind(
            Animate.Motion.Define(m => m
                .Opacity(0, 1)
                .Scale(0.8, 1)
                .Rotate(0, 180)
                .WithDuration(800)
                .WithEasing(Easing.CubicOut)),
            _box);
    }

    void OnScrub(double value)
    {
        SetState(s => s.T = value);
        _player?.SeekFraction(value);
    }

    async Task Play()
    {
        await _player.ForwardAsync();
        if (_player?.IsCompleted() == true)
            SetState(s => s.T = 1);
    }

    void Reset()
    {
        _player?.Reset();
        SetState(s => s.T = 0);
        if (_box is null)
            return;
        _box.Opacity = 0;
        _box.ScaleX = 0.8;
        _box.ScaleY = 0.8;
        _box.Rotation = 0;
    }
}
