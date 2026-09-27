namespace Sample.Components;

sealed class CircleDetailPage : Component
{
    MotionPlayer? _player;

    protected override void OnMounted()
    {
        Animate.Page.HeroInFlight += OnHeroInFlight;
        base.OnMounted();
    }

    protected override void OnWillUnmount()
    {
        Animate.Page.HeroInFlight -= OnHeroInFlight;
        base.OnWillUnmount();
    }

    void OnHeroInFlight(object? sender, HeroTransitionEventArgs e)
    {
        if (e.Kind != HeroTransitionKind.Push)
            return;

        e.At(t =>
        {
            if (t >= 0.7)
                _player.Forward();
        });
    }

    public override VisualNode Render()
        => ContentPage(
            Grid("120, auto, *", "*",
                BoxView()
                    .WidthRequest(88)
                    .HeightRequest(88)
                    .CornerRadius(44)
                    .BackgroundColor(Colors.OrangeRed)
                    .HCenter()
                    .VCenter()
                    .Hero("orb")
                    .GridRow(0),

                VStack(
                    Label("From the center")
                        .FontSize(28),
                    Label("The circle flew here from the middle of the previous page. This block slides in from the left.")
                        .FontSize(16)
                        .Margin(0, 8, 0, 0),
                    Button("Back", async () => await Animate.Page.PopAsync())
                        .HStart()
                        .Margin(0, 24, 0, 0)
                )
                .Padding(24)
                .Opacity(0)
                .BindMotion(
                    Animate.Motion.Define(m => m
                        .FadeIn()
                        .TranslateX(-100, 0)
                        .WithDuration(300)),
                    player => _player = player)
                .GridRow(1)
            )
        )
        .HideNavigationBar();
}
