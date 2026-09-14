namespace Reactor.Animate.Page;

internal static class Geometry
{
    public static Rect GetWindowBounds(VisualElement view)
    {
        var location = GetWindowLocation(view);
        return new Rect(location.X, location.Y, view.Bounds.Width, view.Bounds.Height);
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

    public static bool IsUnder(Element element, Element root)
    {
        for (Element? current = element; current is not null; current = current.Parent)
        {
            if (ReferenceEquals(current, root))
                return true;
        }

        return false;
    }
}
