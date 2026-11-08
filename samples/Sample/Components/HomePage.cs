namespace Sample.Components;

/// <summary>
/// A list of the sample pages, grouped into Hero and Motion.
/// </summary>
sealed class HomePage : Component
{
    static readonly Color Ink = Color.FromArgb("#1C1C1E");
    static readonly Color Paper = Color.FromArgb("#F7F5F2");
    static readonly Color Night = Color.FromArgb("#111110");
    static readonly Color Muted = Color.FromArgb("#8E8E93");

    public override VisualNode Render()
    {
        var ink = Theme.IsDarkTheme ? Paper : Ink;
        return ContentPage("Reactor.Animate",
            ScrollView(
                VStack(
                    Label("Reactor.Animate")
                        .FontSize(22)
                        .TextColor(ink),
                    Label("Page heroes, gallery, photos, nested scrolls, circle, push and pop, and delay. Playground, stagger, scrub, color, timeline, path, and matrix.")
                        .FontSize(16)
                        .TextColor(Muted)
                        .Margin(0, 8, 0, 0),
                    Section("Hero"),
                    VStack(HeroRows(ink))
                        .Spacing(0)
                        .Margin(0, 8, 0, 0),
                    Section("Motion"),
                    VStack(MotionRows(ink))
                        .Spacing(0)
                        .Margin(0, 8, 0, 0)
                )
                .Spacing(0)
                .Padding(16)
            )
            .VerticalScrollBarVisibility(ScrollBarVisibility.Never)
        )
        .BackgroundColor(Theme.IsDarkTheme ? Night : Paper)
        .HideNavigationBar();
    }

    static MauiReactor.Label Section(string title)
        => Label(title)
            .FontSize(16)
            .TextColor(Muted)
            .Margin(0, 24, 0, 0);

    static VisualNode[] HeroRows(Color ink) =>
    [
        Row("Page heroes", () => PageNavigation.Push<HeroesPage>(), ink),
        Row("Gallery", () => PageNavigation.Push<GalleryPage>(), ink),
        Row("Photos", () => PageNavigation.Push<PhotoPage>(), ink),
        Row("Nested scrolls", () => PageNavigation.Push<NestedScrollPage>(), ink),
        Row("Circle to top", () => PageNavigation.Push<CirclePage>(), ink),
        Row("Push and pop timing", () => PageNavigation.Push<PushPopPage>(), ink),
        Row("Delay", () => PageNavigation.Push<DelayPage>(), ink),
    ];

    static VisualNode[] MotionRows(Color ink) =>
    [
        Row("Playground", () => PageNavigation.Push<MotionPlaygroundPage>(), ink),
        Row("Stagger grid", () => PageNavigation.Push<StaggerGridPage>(), ink),
        Row("Scrub", () => PageNavigation.Push<MotionScrubPage>(), ink),
        Row("Color HSV vs RGB", () => PageNavigation.Push<MotionColorPage>(), ink),
        Row("Timeline seek", () => PageNavigation.Push<MotionTimelinePage>(), ink),
        Row("Path", () => PageNavigation.Push<MotionPathPage>(), ink),
        Row("Matrix", () => PageNavigation.Push<MotionTransformPage>(), ink),
    ];

    static MauiReactor.Grid Row(string title, Action open, Color ink)
        => Grid("44", "*,auto",
                Label(title)
                    .FontSize(16)
                    .TextColor(ink)
                    .VCenter(),
                Chevron()
                    .GridColumn(1)
                    .VCenter()
            )
            .HFill()
            .BackgroundColor(Colors.Transparent)
            .OnTapped(open);

    static MauiReactor.Image Chevron()
        => Image(PhosphorIcons.CaretRight)
            .Width(20)
            .Height(20)
            .Aspect(Aspect.AspectFit)
            .InputTransparent(inputTransparent: true);
}
