namespace Sample.Components;

sealed class RecipePageProps
{
    public string Title { get; set; } = "";

    public string Body { get; set; } = "";

    public Color Accent { get; set; } = Colors.MediumPurple;
}

sealed class RecipePage : Component<RecipePage.EmptyState, RecipePageProps>
{
    public class EmptyState;

    public override VisualNode Render()
        => ContentPage(
            Grid(
                VStack(
                    BoxView()
                        .HeightRequest(160)
                        .CornerRadius(24)
                        .BackgroundColor(Props.Accent),

                    Label(Props.Title)
                        .FontSize(28)
                        .HCenter()
                        .Margin(0, 16, 0, 0),

                    Label(Props.Body)
                        .FontSize(16)
                        .HCenter()
                        .HorizontalTextAlignment(TextAlignment.Center),

                    Button("Back", async () => await Animate.Page.PopAsync())
                        .HCenter()
                        .Margin(0, 16)
                )
                .Padding(24)
                .Center()
            )
            .BackgroundColor(Colors.White)
        )
        .HasNavigationBar(false)
        .OnBackButtonPressed(() => Animate.Page.PopAsync(), () => true);
}
