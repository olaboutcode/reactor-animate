using Reactor.Animate;

namespace Sample.Components;

sealed class DetailPage : Component
{
    public override VisualNode Render()
        => ContentPage(
            VStack(
                BoxView()
                    .WidthRequest(280)
                    .HeightRequest(280)
                    .CornerRadius(140)
                    .BackgroundColor(Colors.MediumPurple)
                    .Hero("box"),

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
