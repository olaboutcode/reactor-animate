using Reactor.Animate;

namespace Sample.Components;

sealed class HomePage : Component
{
    public override VisualNode Render()
        => ContentPage(
            Grid(
                Grid(
                    BoxView()
                        .WidthRequest(72)
                        .HeightRequest(72)
                        .CornerRadius(8)
                        .BackgroundColor(Colors.CornflowerBlue)
                        .Hero("from_corner")
                )
                .HStart()
                .VStart(),

                VStack(
                    BoxView()
                        .WidthRequest(70)
                        .HeightRequest(70)
                        .CornerRadius(12)
                        .BackgroundColor(Colors.OrangeRed)
                        .HCenter()
                        .Hero("box"),

                    Label("Reactor.Animate")
                        .FontSize(28)
                        .HCenter(),

                    Label("Orange grows from center. Blue grows from its top-left.")
                        .FontSize(16)
                        .HCenter(),

                    Button("Open", async () => await Open())
                )
                .Spacing(20)
                .Center()
            )
            .Padding(24)
        )
        .HasNavigationBar(false);

    static Task<MauiControls.Page> Open()
        => Animate.Page.PushAsync<DetailPage>(t => t
            .Hero("box", h => h.AnchorCenter())
            .Hero("from_corner", h => h.AnchorTopLeft())
            .WithEasing(Easing.CubicInOut));
}
