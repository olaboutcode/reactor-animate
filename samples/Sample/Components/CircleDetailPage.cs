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
        if (!e.IsPushTransition())
            return;

        e.At(t =>
        {
            if (t >= 0.7)
                _player.Forward();
        });
    }

    public override VisualNode Render()
        => ContentPage(
            Grid("auto,*", "*",
                NavigationBar
                    .WithBackButton()
                    .MiddleView(Label("Circle Details Page").FontSize(FontSizing.Title))
                    .GridRow(0),
                
                Grid("*,*", "*",
                    BoxView()
                        .WidthRequest(100)
                        .HeightRequest(100)
                        .CornerRadius(Radius.Large)
                        .BackgroundColor(Colors.OrangeRed)
                        .Center()
                        .Hero("orb")
                        .GridRow(0),

                    VStack(
                        Label("From the center")
                        .FontSize(FontSizing.Title),

                        Label(@"The circle flew here from the middle of the previous page.This block slides in from the left.")
                        .FontSize(FontSizing.Body)
                    )
                    .Spacing(Spacing.Small)
                    .Opacity(0)
                    .BindMotion(
                        Animate.Motion.Define(m => m
                            .FadeIn()
                            .TranslateX(-100, 0)
                            .WithDuration(300)),
                        player => _player = player)
                    .GridRow(1)
                )
                .GridRow(1)
                .Padding(Spacing.Medium)
            )
        )
        .HideNavigationBar();
}
