using Reactor.Animate;

namespace Sample.Components;

sealed class DetailPage : Component
{
    public override VisualNode Render()
        => ContentPage(
            Grid(
                Grid(
                    BoxView()
                        .WidthRequest(220)
                        .HeightRequest(220)
                        .CornerRadius(28)
                        .BackgroundColor(Colors.MediumPurple)
                        .Hero("from_corner")
                )
                .HStart()
                .VStart(),

                VStack(
                    BoxView()
                        .WidthRequest(100)
                        .HeightRequest(100)
                        .CornerRadius(50)
                        .BackgroundColor(Colors.OrangeRed)
                        .HCenter()
                        .Hero("box"),

                    Label("Shared element")
                        .FontSize(28)
                        .HCenter(),

                    Label("The blue square scaled from its top-left corner.")
                        .FontSize(16)
                        .HCenter(),

                    Button("Back", async () => await Animate.Page.PopAsync())
                )
                .Spacing(20)
                .Center()
            )
            .Padding(24)
        )
        .HasNavigationBar(false)
        .OnBackButtonPressed(() => Animate.Page.PopAsync(), () => true);
}
