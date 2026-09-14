using Reactor.Animate;

namespace Sample.Components;

sealed class DetailPage : Component
{
    public override VisualNode Render()
        => ContentPage(
            Grid(
                BoxView()
                    .HeightRequest(220)
                    .CornerRadius(28)
                    .BackgroundColor(Colors.MediumPurple)
                    .VStart()
                    .Hero("cover"),

                VStack(
                    Label("Shared element")
                        .FontSize(28)
                        .HCenter(),

                    Label("The orange box flew up and filled this header.")
                        .FontSize(16)
                        .HCenter(),

                    Button("Back", async () => await Animate.Page.PopAsync())
                )
                .Spacing(20)
                .Center()
            )
        )
        .HasNavigationBar(false)
        .OnBackButtonPressed(() => Animate.Page.PopAsync(), () => true);
}
