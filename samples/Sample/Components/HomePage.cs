using Reactor.Animate;

namespace Sample.Components;

sealed class HomePage : Component
{
    public override VisualNode Render()
        => ContentPage(
            VStack(
                BoxView()
                    .WidthRequest(120)
                    .HeightRequest(120)
                    .BackgroundColor(Colors.CornflowerBlue)
                    .HCenter()
                    .OnTapped(async () => await HomePage.Open())
                    .Hero("box"),

                Label("Reactor.Animate")
                    .FontSize(28)
                    .HCenter(),

                Label("Tap the box. It flies into the next page.")
                    .FontSize(16)
                    .HCenter(),

                Button("Open", async () => await Open())
            )
            .Spacing(20)
            .Padding(24)
            .Center()
        )
        .HasNavigationBar(false);

    static Task<MauiControls.Page> Open()
        => Animate.Page.PushAsync<DetailPage>(t => t.Hero("box"));
}
