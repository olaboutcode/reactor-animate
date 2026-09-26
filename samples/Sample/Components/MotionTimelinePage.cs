namespace Sample.Components;

sealed class MotionTimelinePage : Component
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
                NavigationBar
                    .BackNavigation("Home")
                    .GridRow(0),
                VStack(
                    Label("Timeline")
                        .FontSize(28)
                        .HCenter(),
                    Label("Fade+slide, then scale. Seek jumps to a named child.")
                        .FontSize(14)
                        .HCenter()
                        .TextColor(Colors.Gray),
                    BoxView(b => _box = b)
                        .HeightRequest(96)
                        .WidthRequest(96)
                        .CornerRadius(16)
                        .BackgroundColor(Colors.CornflowerBlue)
                        .HCenter()
                        .OnLoaded(Bind),
                    HStack(
                        Button("Play", () => _ = Play()),
                        Button("Intro", () => _player?.Seek("intro")),
                        Button("Pulse", () => _player?.Seek("pulse"))
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
        ApplyRest();
        var intro = Motion.None.FadeIn().SlideIn(SlideFrom.Left, 48).WithDuration(280).WithEasing(Easing.CubicOut);
        var pulse = Motion.None.Scale(1, 1.12).WithDuration(200).WithEasing(Easing.CubicOut);
        _player = Animate.Motion.Bind(
            Motion.None.WithDuration(280)
                .Add(intro, 0, "intro")
                .Then(pulse, "pulse"),
            _box);
    }

    async Task Play()
    {
        if (_player is null)
            return;
        ApplyRest();
        try
        {
            await _player.ForwardAsync();
        }
        catch (OperationCanceledException)
        {
        }
    }

    void ApplyRest()
    {
        if (_box is null)
            return;
        _box.Opacity = 0;
        _box.TranslationX = -48;
        _box.ScaleX = 1;
        _box.ScaleY = 1;
    }
}
