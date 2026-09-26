namespace Sample.Components;

sealed class MotionPlaygroundPage : Component
{
    Microsoft.Maui.Controls.BoxView? _box;
    MotionPlayer? _player;

    public override VisualNode Render()
        => ContentPage(
            VStack(
                Label("Animate.Motion")
                    .FontSize(28)
                    .HCenter(),
                Label("Opacity player — forward, pause, reverse, reset")
                    .FontSize(14)
                    .HCenter()
                    .TextColor(Colors.Gray),
                BoxView(b => _box = b)
                    .HeightRequest(96)
                    .WidthRequest(96)
                    .CornerRadius(16)
                    .BackgroundColor(Colors.OrangeRed)
                    .Opacity(0)
                    .HCenter()
                    .OnLoaded(EnsurePlayer),
                HStack(
                    Button("Forward", async () =>
                    {
                        if (_player is null) return;
                        await _player.ForwardAsync();
                    }),
                    Button("Pause", () => _player?.Pause()),
                    Button("Resume", () => _player?.Resume())
                )
                .Spacing(8)
                .HCenter(),
                HStack(
                    Button("Reverse", async () =>
                    {
                        if (_player is null) return;
                        await _player.ReverseAsync();
                    }),
                    Button("Reset", () => _player?.Reset())
                )
                .Spacing(8)
                .HCenter()
            )
            .Spacing(16)
            .Padding(24)
            .VCenter()
        )
        .HideNavigationBar();

    void EnsurePlayer()
    {
        if (_box is null || _player is not null)
            return;
        _player = Animate.Motion.Bind(
            Animate.Motion.Define(m => m.Opacity(0, 1).WithDuration(600).WithEasing(Easing.CubicOut)),
            _box);
    }
}
