namespace Sample.Components;

public partial class NavigationBar: Component
{
    private Thickness? _padding;

    [Prop]private VisualNode? _leftView;
    [Prop]private VisualNode? _titleView;
    [Prop]private VisualNode? _rightView;
    [Prop]private VisualNode? _searchInput;

    public override VisualNode Render()
    {
        List<string> columnTemplate = [];
        if (_leftView is not null)
            columnTemplate.Add("auto");
        if (_titleView is not null || _searchInput is not null)
            columnTemplate.Add("*");
        if (_rightView is not null && _searchInput is null)
            columnTemplate.Add("auto");

        var grid = (_searchInput is not null)
            ? Grid("*", string.Join(',', columnTemplate),
                WrapView(_leftView)?.GridColumn(0),
                WrapView(_searchInput)
                    ?.HFill()
                    ?.VCenter()
                    ?.GridColumn(1)
            ) 
            : Grid("*", string.Join(',', columnTemplate),
                WrapView(_leftView)?.GridColumn(0),
                WrapView(_titleView)?.GridColumn(1),
                WrapView(_rightView)?.GridColumn(2)
            );

        return grid
            .BackgroundColor(Colors.Transparent)
            .ColumnSpacing(Spacing.Medium)
            .Padding(_padding ?? new Thickness(Spacing.Medium, Spacing.Small));
    }

    static MauiReactor.ContentView? WrapView(VisualNode? view) =>
        view is not null ? ContentView(view).Center() : null;

    public static NavigationBar ReaderSettings() =>
        new()
        {
            _padding = new Thickness(Spacing.Medium, Spacing.XSmall)
        };

    public static NavigationBar Create(string title) =>
        new NavigationBar()
        .TitleView(
            Label(title)
            .FontSize(FontSizing.Title)
        );

    public static NavigationBar BackNavigation(string title) =>
        new NavigationBar()
        .TitleView(
            Label(title)
            .FontSize(FontSizing.Title)
        )
        .LeftView(
            CustomButton
                .BackNavButton()
                .ButtonSize(TouchTarget.Min)
                .OnTapped(async () => await Animate.Page.PopAsync())
        );
}