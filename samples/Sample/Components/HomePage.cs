namespace Sample.Components;

sealed class HomePage : Component
{
    static readonly Color[] BoxColors = RandomLightColors(11);

    static Color[] RandomLightColors(int count)
    {
        var random = Random.Shared;
        var hues = Enumerable.Range(0, count)
            .Select(i => (i + random.NextDouble()) / count)
            .OrderBy(_ => random.Next())
            .ToArray();

        return [.. hues.Select(hue => Color.FromHsla(hue, 0.42, 0.82))];
    }

    public override VisualNode Render()
        => ContentPage(
            ScrollView(
                VStack(
                    Label("Reactor.Animate")
                        .FontSize(28)
                        .HCenter(),

                    AnimationExampleBox(
                        BoxColors[0],
                        "Gallery Heros",
                        "Open a tile and fly it into the detail page.",
                        PageNavigation.Push<GalleryPage>),
                    AnimationExampleBox(
                        BoxColors[1],
                        "Photo Heros",
                        "Open a photo and grow it into the detail.",
                        PageNavigation.Push<PhotoPage>),
                    AnimationExampleBox(
                        BoxColors[2],
                        "Hero on Destination",
                        "Fly the orb, then run a custom animation on the destination.",
                        PageNavigation.Push<CirclePage>),
                    AnimationExampleBox(
                        BoxColors[3],
                        "MultiHeros",
                        "Fly several heroes together, each from its own anchor.",
                        PageNavigation.Push<MultiHerosPage>),
                    AnimationExampleBox(
                        BoxColors[4],
                        "Motion Playground",
                        "Pick a recipe, then play it forward or in reverse.",
                        PageNavigation.Push<MotionPlaygroundPage>),
                    AnimationExampleBox(
                        BoxColors[5],
                        "Stagger Grid",
                        "Animate a grid of items in a staggered manner.",
                        PageNavigation.Push<StaggerGridPage>),
                    AnimationExampleBox(
                        BoxColors[6],
                        "Scrub",
                        "Drag the slider to seek through the motion.",
                        PageNavigation.Push<MotionScrubPage>),
                    AnimationExampleBox(
                        BoxColors[7],
                        "Color HSV vs RGB",
                        "Compare a short hue arc with mixing each channel.",
                        PageNavigation.Push<MotionColorPage>),
                    AnimationExampleBox(
                        BoxColors[8],
                        "Timeline Seek",
                        "Jump to a specific animation within the timeline.",
                        PageNavigation.Push<MotionTimelinePage>),
                    AnimationExampleBox(
                        BoxColors[9],
                        "Path",
                        "Move along a cubic Bézier and a clockwise arc.",
                        PageNavigation.Push<MotionPathPage>),
                    AnimationExampleBox(
                        BoxColors[10],
                        "Matrix",
                        "Turn and tilt a card with a matrix transform.",
                        PageNavigation.Push<MotionTransformPage>),

                    new AnimateButton()
                    .Text("Test Button")
                    .Icon(HeroIcons.ArrowLeftCircle),

                    new AnimateButton()
                    .Icon(HeroIcons.ArrowRight)
                )
                .Spacing(Spacing.Medium)
                .Padding(Spacing.Medium)
            )
            .VerticalScrollBarVisibility(ScrollBarVisibility.Never)
        )
        .HideNavigationBar();

        static MauiReactor.Border AnimationExampleBox(
            Color boxColor,
            string title,
            string desc,
            Action navigate)
         => Border(
            VStack(
                Grid("*", "*,auto",
                    Label(title)
                        .FontSize(FontSizing.Body)
                        .TextTransform(TextTransform.Uppercase)
                        .TextColor(CustomColors.Gray600),
                    Image(HeroIcons.ArrowUpRight)
                        .HeightRequest(IconSizing.XSmall)
                        .WidthRequest(IconSizing.XSmall)
                        .Aspect(Aspect.AspectFit)
                        .GridColumn(1)
                )
                .Margin(0,0,0,Spacing.Small),
                Label(desc)
                    .FontSize(FontSizing.Label)
                    .TextColor(CustomColors.Gray600)
            )
            .Spacing(Spacing.Small)
         )
         .Padding(Spacing.Large)
         .Stroke(Colors.Transparent)
         .StrokeCornerRadius(Radius.Medium)
         .BackgroundColor(boxColor)
         .OnTapped(navigate);
}
