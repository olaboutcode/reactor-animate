namespace Sample.Components;

sealed class NestedCardProps
{
    public string Name { get; set; } = "";

    public Color Color { get; set; } = Colors.Coral;
}

/// <summary>
/// A horizontal row of heroes inside the page scroll. The row scroll stays
/// clipped, and the section titles stay under the bar, while a card flies out.
/// </summary>
sealed class NestedScrollPage : Component
{
    static readonly (string Title, NestedCardProps[] Cards)[] Sections =
    [
        ("Morning", [
            Card("Dawn", Colors.OrangeRed),
            Card("Amber", Colors.Goldenrod),
            Card("Coral", Colors.Coral),
            Card("Peach", Colors.SandyBrown),
        ]),
        ("Afternoon", [
            Card("Teal", Colors.Teal),
            Card("Sky", Colors.DeepSkyBlue),
            Card("Mint", Colors.MediumSeaGreen),
            Card("Sea", Colors.CadetBlue),
        ]),
        ("Evening", [
            Card("Plum", Colors.MediumPurple),
            Card("Indigo", Colors.Indigo),
            Card("Rose", Colors.HotPink),
            Card("Wine", Colors.Brown),
        ]),
        ("Night", [
            Card("Dusk", Colors.DarkSlateBlue),
            Card("Ink", Colors.MidnightBlue),
            Card("Moon", Colors.LightSteelBlue),
            Card("Fog", Colors.DimGray),
        ]),
        ("Late", [
            Card("Ash", Colors.Gray),
            Card("Coal", Colors.Black),
            Card("Slate", Colors.SlateGray),
            Card("Storm", Colors.DarkCyan),
        ]),
    ];

    public override VisualNode Render()
        => ContentPage(
            Grid("auto,*", "*",
                NavigationBar
                    .BackNavigation("Nested scrolls")
                    .GridRow(0),

                ScrollView(
                    VStack(Rows())
                        .Spacing(24)
                        .Padding(0, 12)
                )
                .Orientation(ScrollOrientation.Vertical)
                .VerticalOptions(LayoutOptions.Fill)
                .HorizontalOptions(LayoutOptions.Fill)
                .GridRow(1)
            )
        )
        .HideNavigationBar()
        .SafeAreaEdges(SafeAreaEdges.All);

    static VisualNode[] Rows()
    {
        var rows = new List<VisualNode>
        {
            Label("Section titles stay under the bar.")
                .FontSize(FontSizing.Body)
                .Margin(16, 0),
        };

        foreach (var (title, cards) in Sections)
            rows.Add(Section(title, cards));

        return rows.ToArray();
    }

    static VStack Section(string title, NestedCardProps[] cards)
        => VStack(
            Label(title)
                .FontSize(FontSizing.Heading)
                .Margin(16, 0),
            ScrollView(
                HStack(cards.Select(Tile).ToArray())
                    .Spacing(12)
                    .Padding(16, 0)
            )
            .Orientation(ScrollOrientation.Horizontal)
            .HeightRequest(112)
            .HorizontalScrollBarVisibility(ScrollBarVisibility.Never)
            .VerticalScrollBarVisibility(ScrollBarVisibility.Never)
        )
        .Spacing(8);

    static VisualNode Tile(NestedCardProps card)
        => BoxView()
            .WidthRequest(148)
            .HeightRequest(112)
            .CornerRadius(16)
            .BackgroundColor(card.Color)
            .OnTapped(async () => await Open(card))
            .Hero(card.Name);

    static Task<MauiControls.Page> Open(NestedCardProps card)
        => Animate.Page.PushAsync<NestedScrollDetailPage, NestedCardProps>(t => t
            .Hero(card.Name, h => h.AnchorCenter())
            .WithoutChromeFade()
            .WithEasing(Easing.CubicOut)
            .WithDuration(300),
            props =>
            {
                props.Name = card.Name;
                props.Color = card.Color;
            });

    static NestedCardProps Card(string name, Color color)
        => new() { Name = name, Color = color };
}
