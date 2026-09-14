using Reactor.Animate;

namespace Sample.Components;

sealed class HomePage : Component
{
    public override VisualNode Render()
        => ContentPage(
            VStack(
                VStack(
                    BoxView()
                        .WidthRequest(70)
                        .HeightRequest(70)
                        .CornerRadius(14)
                        .BackgroundColor(Colors.OrangeRed)
                        .Hero("box"),

                    BoxView()
                        .WidthRequest(120)
                        .HeightRequest(120)
                        .CornerRadius(24)
                        .BackgroundColor(Colors.CornflowerBlue)
                        .Hero("another_box")
                )
                .Spacing(10)
                .Center(),

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
        => Animate.Page.PushAsync<DetailPage>(t => t
            .Hero("box", "another_box")
            .Translate()
            .AnchorCenter()
            .WithEasing(Easing.CubicInOut));
}
