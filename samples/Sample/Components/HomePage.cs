using Reactor.Animate;

namespace Sample.Components;

sealed class HomePage : Component
{
    public override VisualNode Render()
        => ContentPage(
            VStack(
                BoxView()
                    .WidthRequest(70)
                    .HeightRequest(70)
                    .CornerRadius(12)
                    .BackgroundColor(Colors.OrangeRed)
                    .HCenter()
                    .Hero("cover"),

                Label("Reactor.Animate")
                    .FontSize(28)
                    .HCenter(),

                Label("Tap Open. The orange box fills the top of the next page.")
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
            .Hero("cover", h => h.AnchorCenter())
            .WithEasing(Easing.CubicInOut));
}
