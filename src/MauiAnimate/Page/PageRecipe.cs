namespace Reactor.Animate;

/// <summary>
/// Edge a page slides in from. Pop uses the opposite edge.
/// </summary>
public enum SlideEdge
{
    None,
    Left,
    Right,
    Up,
    Down,
}

internal readonly record struct PageRecipe(
    bool Fade,
    SlideEdge Slide,
    double ScaleFrom,
    string? ExpandTag)
{
    public static PageRecipe Empty { get; } = new(false, SlideEdge.None, 1, null);

    public bool HasMotion
        => Fade || Slide != SlideEdge.None || ScaleFrom != 1 || !string.IsNullOrEmpty(ExpandTag);

    public PageRecipe Negate()
        => this with
        {
            Slide = Slide switch
            {
                SlideEdge.Left => SlideEdge.Right,
                SlideEdge.Right => SlideEdge.Left,
                SlideEdge.Up => SlideEdge.Down,
                SlideEdge.Down => SlideEdge.Up,
                _ => Slide,
            },
        };

    public static PageRecipe Merge(PageRecipe left, PageRecipe right)
        => new(
            Fade: left.Fade || right.Fade,
            Slide: right.Slide != SlideEdge.None ? right.Slide : left.Slide,
            ScaleFrom: right.ScaleFrom != 1 ? right.ScaleFrom : left.ScaleFrom,
            ExpandTag: right.ExpandTag ?? left.ExpandTag);
}
