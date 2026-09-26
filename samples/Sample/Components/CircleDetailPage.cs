namespace Sample.Components;

sealed class CircleDetailPageState
{
    public bool IsVisible { get; set; }
    public double Progress { get; set; }
}

sealed class CircleDetailPage : Component<CircleDetailPageState>
{
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

        e.At(t => SetState(s =>
        {
            s.Progress = t;
            if (t >= 0.3)
                s.IsVisible = true;
        }));
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

                Label($"Progress At {State.Progress}")
                    .Padding(24)
                    .GridRow(1),

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
                .Opacity(State.IsVisible ? 1 : 0)
                .TranslationX(State.IsVisible  ? 0 : -100)
                .WithAnimation(duration: 300)
                .GridRow(2)
            )
        )
        .HideNavigationBar();
}
