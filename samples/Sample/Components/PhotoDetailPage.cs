namespace Sample.Components;

sealed class PhotoDetailPage : Component<PhotoDetailPage.EmptyState, PhotoItemProps>
{
    public class EmptyState;

    public override VisualNode Render()
        => ContentPage(
            VStack(
                Image(Props.Source)
                    .Aspect(Aspect.AspectFill)
                    .HeightRequest(280)
                    .Hero($"photo-{Props.Id}"),

                Label(Props.Title)
                    .FontSize(28)
                    .HCenter()
                    .Margin(0, 24, 0, 0),

                Label("Same Image source and AspectFill on both pages, so FLIP scales the view rect.")
                    .FontSize(16)
                    .HCenter()
                    .Padding(24, 8),

                Button("Back", async () => await Animate.Page.PopAsync())
                    .HCenter()
                    .Margin(0, 16)
            )
        )
        .HideNavigationBar();
}
