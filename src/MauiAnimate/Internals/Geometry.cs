using Reactor.Animate;
using Microsoft.Maui.Platform;

namespace Reactor.Animate.Internals;

/// <summary>
/// Window-space bounds of a view, used to measure a hero before the page changes.
/// Prefers the native frame. Otherwise walks <c>Bounds</c> plus translation and
/// scroll offsets up to the window.
/// </summary>
internal static class Geometry
{
    public static Rect GetWindowBounds(VisualElement view)
    {
        var native = NativeBounds(view);
        if (native is { Width: > 0, Height: > 0 })
            return native.Value;

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

    static Rect? NativeBounds(VisualElement view)
    {
        var native = view.Handler?.PlatformView
            ?? (view.Handler?.MauiContext is { } context ? view.ToPlatform(context) : null);
        if (native is null)
            return null;

#if IOS || MACCATALYST
        if (native is UIKit.UIView uiView)
        {
            uiView.Superview?.LayoutIfNeeded();
            var window = uiView.Window;
            if (window is null)
                return null;
            var rect = uiView.ConvertRectToView(uiView.Bounds, window);
            if (rect.Width <= 0 || rect.Height <= 0)
                return null;
            return new Rect(rect.X, rect.Y, rect.Width, rect.Height);
        }
#elif ANDROID
        if (native is Android.Views.View androidView && androidView.Width > 0 && androidView.Height > 0)
        {
            var density = androidView.Resources?.DisplayMetrics?.Density ?? 1f;
            if (density <= 0)
                density = 1f;
            var loc = new int[2];
            androidView.GetLocationOnScreen(loc);
            return new Rect(loc[0] / density, loc[1] / density, androidView.Width / density, androidView.Height / density);
        }
#elif WINDOWS
        if (native is Microsoft.UI.Xaml.FrameworkElement fe
            && fe.XamlRoot?.Content is Microsoft.UI.Xaml.UIElement root
            && fe.ActualWidth > 0 && fe.ActualHeight > 0)
        {
            try
            {
                var origin = fe.TransformToVisual(root).TransformPoint(new Windows.Foundation.Point(0, 0));
                return new Rect(origin.X, origin.Y, fe.ActualWidth, fe.ActualHeight);
            }
            catch (Exception)
            {
                return null;
            }
        }
#endif
        return null;
    }
}
