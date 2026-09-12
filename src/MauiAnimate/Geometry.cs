namespace Reactor.Animate;

internal static class Geometry
{
    public static Rect GetWindowBounds(VisualElement view)
    {
        var location = GetWindowLocation(view);
        return new Rect(location.X, location.Y, view.Width, view.Height);
    }

    public static Rect MapTo(Rect windowBounds, VisualElement destinationSpace)
    {
        var origin = GetWindowLocation(destinationSpace);
        return new Rect(
            windowBounds.X - origin.X,
            windowBounds.Y - origin.Y,
            windowBounds.Width,
            windowBounds.Height);
    }

    public static Point GetWindowLocation(VisualElement view)
    {
        double x = 0;
        double y = 0;
        Element? current = view;

        while (current is VisualElement visual)
        {
            x += visual.Bounds.X + visual.TranslationX;
            y += visual.Bounds.Y + visual.TranslationY;

            if (visual is ScrollView scroll)
            {
                x -= scroll.ScrollX;
                y -= scroll.ScrollY;
            }

            current = visual.Parent;
        }

        return new Point(x, y);
    }
}
