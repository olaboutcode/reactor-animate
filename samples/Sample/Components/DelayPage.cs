namespace Sample.Components;

sealed class DelayPage : Component
{
    MauiControls.BoxView? _card;
    MotionPlayer? _player;

    public override VisualNode Render()
        => ContentPage(
            Grid("auto,*", "*",
                NavigationBar
                    .BackNavigation("Home")
                    .GridRow(0),

                ScrollView(
                    VStack(
                        Label("Motion")
                            .FontSize(16)
                            .HCenter(),

                        BoxView(box => _card = box)
                            .WidthRequest(88)
                            .HeightRequest(88)
                            .CornerRadius(20)
                            .BackgroundColor(Colors.DarkOrange)
                            .Opacity(0)
                            .TranslationY(28)
                            .HCenter(),

                        Label("The card waits 700 ms, then fades and rises over 400 ms.")
                            .FontSize(16)
                            .HorizontalTextAlignment(TextAlignment.Center),

                        Button("Play", Play)
                            .HCenter(),

                        Label("Hero")
                            .FontSize(16)
                            .HCenter()
                            .Margin(0, 12, 0, 0),

                        BoxView()
                            .WidthRequest(88)
                            .HeightRequest(88)
                            .CornerRadius(20)
                            .BackgroundColor(Colors.Teal)
                            .HCenter()
                            .OnTapped(Open)
                            .Hero("delay"),

                        Label("Tap the tile. Push waits 700 ms, then flies for 400 ms. Back starts at once.")
                            .FontSize(16)
                            .HorizontalTextAlignment(TextAlignment.Center)
                    )
                    .Spacing(16)
                    .Padding(24)
                )
                .GridRow(1)
            )
        )
        .HideNavigationBar();

    void Play()
    {
        if (_card is null)
            return;

        _player?.Dispose();
        _player = Animate.Motion.Play(
            m => m.Opacity(0, 1).TranslateY(28, 0).WithDuration(400).WithDelay(700),
            _card);
    }

    static Task Open()
        => Animate.Page.PushAsync<DelayDetailPage>(t => t
            .Hero("delay", h => h.AnchorCenter())
            .WithDuration(400)
            .WithDelay(700)
            .WithPopDelay(0));
}
