using Reactor.Animate;

namespace Sample.Components;

sealed class DetailPage : Component
{
    public override VisualNode Render()
        => ContentPage(
            VStack(
                VStack(
                    BoxView()
                        .WidthRequest(140)
                        .HeightRequest(140)
                        .CornerRadius(0)
                        .BackgroundColor(Colors.GreenYellow)
                        .Hero("box"),

                    BoxView()
                        .WidthRequest(120)
                        .HeightRequest(120)
                        .CornerRadius(8)
                        .BackgroundColor(Colors.CornflowerBlue)
                        .Hero("another_box")
                )
                .Center()
                .Spacing(10),

                Label("Shared element")
                    .FontSize(28)
                    .HCenter(),

                Label("The box flew from the list into this page.")
                    .FontSize(16)
                    .HCenter(),

                Button("Back", async () => await Animate.Page.PopAsync())
            )
            .Spacing(20)
            .Padding(24)
            .Center()
        )
        .HasNavigationBar(false)
        .OnBackButtonPressed(() => Animate.Page.PopAsync(), () => true);
}
