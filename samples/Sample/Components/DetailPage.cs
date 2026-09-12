using Reactor.Animate;

namespace Sample.Components;

class DetailProps
{
    public string Title { get; set; } = "";

    public string Body { get; set; } = "";

    public bool ShowHero { get; set; } = true;
}

class DetailPageState
{
}

sealed class DetailPage : Component<DetailPageState, DetailProps>
{
    public override VisualNode Render()
        => ContentPage(
            VStack(
                Props.ShowHero
                    ? new Hero("box")
                    {
                        BoxView()
                            .WidthRequest(100)
                            .HeightRequest(100)
                            .BackgroundColor(Colors.CornflowerBlue)
                    }
                    : BoxView()
                        .WidthRequest(100)
                        .HeightRequest(100)
                        .BackgroundColor(Colors.CornflowerBlue)
                        .Opacity(0.35),

                Label(Props.Title)
                    .FontSize(28)
                    .HCenter(),

                Label(Props.Body)
                    .FontSize(16)
                    .HCenter(),

                Button("Back", async () => await Nav.PopAsync(Navigation))
            )
            .Spacing(20)
            .Padding(24)
            .Center()
        )
        .HasNavigationBar(false)
        .OnBackButtonPressed(() => Nav.PopAsync(Navigation), () => true);
}
