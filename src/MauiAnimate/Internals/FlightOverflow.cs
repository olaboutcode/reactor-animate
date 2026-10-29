using Reactor.Animate;
using Microsoft.Maui.Platform;

namespace Reactor.Animate.Internals;

/// <summary>
/// Lets flying heroes paint outside clipping parents. iOS and Android do not
/// use this. <see cref="FlightStage"/> draws those flights in a window overlay
/// so a nested scroll keeps its clip. This path is the desktop fallback.
/// Restored on dispose.
/// </summary>
internal sealed class FlightOverflow : IDisposable
{
    const int FlightZIndex = 10_000;

    readonly List<Action> _restore = [];
    readonly HashSet<object> _seen = [];
    bool _disposed;

    public FlightOverflow(IEnumerable<VisualElement> heroes)
    {
        var flying = heroes as IReadOnlyList<VisualElement> ?? heroes.ToList();
        foreach (var hero in flying)
        {
            Raise(hero);
            UnclipLayouts(hero);
            UnclipNative(hero);
        }

        // After the ancestors are open. The hero stays in place, so the flight
        // transform keeps the window position it already measured.
        HoldScrollSiblings(flying);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        for (var i = _restore.Count - 1; i >= 0; i--)
        {
            try
            {
                _restore[i]();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        _restore.Clear();
        _seen.Clear();
    }

    void Raise(VisualElement hero)
    {
        for (Element? current = hero; current is VisualElement visual && current is not Microsoft.Maui.Controls.Page; current = visual.Parent)
        {
            if (!_seen.Add(visual))
                continue;

            var previous = visual.ZIndex;
            if (previous >= FlightZIndex)
                continue;

            visual.ZIndex = FlightZIndex;
            visual.Handler?.UpdateValue(nameof(VisualElement.ZIndex));
            _restore.Add(() =>
            {
                visual.ZIndex = previous;
                visual.Handler?.UpdateValue(nameof(VisualElement.ZIndex));
            });
        }
    }

    void UnclipLayouts(VisualElement hero)
    {
        for (Element? current = hero.Parent; current is not null; current = current.Parent)
        {
            if (current is not Layout layout || !_seen.Add(("layout", layout)))
                continue;

            if (!layout.IsClippedToBounds)
                continue;

            layout.IsClippedToBounds = false;
            layout.Handler?.UpdateValue(nameof(Layout.IsClippedToBounds));
            _restore.Add(() =>
            {
                layout.IsClippedToBounds = true;
                layout.Handler?.UpdateValue(nameof(Layout.IsClippedToBounds));
            });
        }
    }

    // UnclipIos, UnclipAndroid, and UnclipWindows use instance state. On net10.0
    // those calls are compiled out, so the method looks stateless to CA1822.
#pragma warning disable CA1822
    void UnclipNative(VisualElement hero)
    {
        var native = hero.Handler?.PlatformView
            ?? (hero.Handler?.MauiContext is { } context ? hero.ToPlatform(context) : null);
        if (native is null)
            return;

#if IOS || MACCATALYST
        if (native is UIKit.UIView uiView)
            UnclipIos(uiView.Superview);
#elif ANDROID
        if (native is Android.Views.View androidView)
            UnclipAndroid(androidView.Parent as Android.Views.View);
#elif WINDOWS
        if (native is Microsoft.UI.Xaml.UIElement element)
            UnclipWindows(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(element) as Microsoft.UI.Xaml.UIElement);
#endif
    }
#pragma warning restore CA1822

    void HoldScrollSiblings(IReadOnlyList<VisualElement> flying)
    {
#if IOS || MACCATALYST
        var views = new List<UIKit.UIView>(flying.Count);
        foreach (var hero in flying)
        {
            if (PlatformView(hero) is UIKit.UIView view)
                views.Add(view);
        }

        if (views.Count > 0)
            HoldIosScrollSiblings(views);
#elif ANDROID
        var views = new List<Android.Views.View>(flying.Count);
        foreach (var hero in flying)
        {
            if (PlatformView(hero) is Android.Views.View view)
                views.Add(view);
        }

        if (views.Count > 0)
            HoldAndroidScrollSiblings(views);
#else
        _ = flying;
        _ = _restore;
#endif
    }

    static object? PlatformView(VisualElement hero)
        => hero.Handler?.PlatformView
            ?? (hero.Handler?.MauiContext is { } context ? hero.ToPlatform(context) : null);

#if IOS || MACCATALYST
    void UnclipIos(UIKit.UIView? start)
    {
        for (var current = start; current is not null && current is not UIKit.UIWindow; current = current.Superview)
        {
            if (!_seen.Add(current))
                continue;

            var clips = current.ClipsToBounds;
            var masks = current.Layer.MasksToBounds;
            if (!clips && !masks)
                continue;

            current.ClipsToBounds = false;
            current.Layer.MasksToBounds = false;
            _restore.Add(() =>
            {
                current.ClipsToBounds = clips;
                current.Layer.MasksToBounds = masks;
            });
        }
    }

    void HoldIosScrollSiblings(List<UIKit.UIView> heroes)
    {
        var keep = new HashSet<UIKit.UIView>();
        var scrolls = new List<UIKit.UIScrollView>();
        foreach (var hero in heroes)
        {
            for (var current = hero; current is not null; current = current.Superview)
                keep.Add(current);

            for (var current = hero.Superview; current is not null; current = current.Superview)
            {
                if (current is UIKit.UIScrollView scroll && !scrolls.Contains(scroll))
                    scrolls.Add(scroll);
            }
        }

        if (scrolls.Count == 0)
            return;

        var masked = new HashSet<UIKit.UIView>();
        foreach (var hero in heroes)
        {
            UIKit.UIView? top = null;
            for (var current = hero.Superview; current is not null; current = current.Superview)
            {
                if (current is UIKit.UIScrollView)
                    top = current;
            }

            for (UIKit.UIView? current = hero; current is not null && current != top; current = current.Superview)
            {
                var parent = current.Superview;
                if (parent is null)
                    break;

                foreach (var sibling in parent.Subviews)
                {
                    if (keep.Contains(sibling) || !masked.Add(sibling))
                        continue;

                    MaskIosSibling(sibling, parent, scrolls);
                }
            }
        }
    }

    void MaskIosSibling(UIKit.UIView sibling, UIKit.UIView parent, List<UIKit.UIScrollView> scrolls)
    {
        CoreGraphics.CGRect? clip = null;
        foreach (var scroll in scrolls)
        {
            if (!IosContains(scroll, parent))
                continue;

            var viewport = scroll.ConvertRectToView(scroll.Bounds, sibling);
            clip = clip is null ? viewport : CoreGraphics.CGRect.Intersect(clip.Value, viewport);
        }

        if (clip is null)
            return;

        var bounds = sibling.Layer.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var mask = new CoreAnimation.CAShapeLayer
        {
            Frame = new CoreGraphics.CGRect(0, 0, bounds.Width, bounds.Height),
        };
        var path = new CoreGraphics.CGPath();
        path.AddRect(clip.Value);
        mask.Path = path;
        var previous = sibling.Layer.Mask;
        sibling.Layer.Mask = mask;
        _restore.Add(() =>
        {
            if (sibling.Layer.Mask == mask)
                sibling.Layer.Mask = previous;
            path.Dispose();
            mask.Dispose();
        });
    }

    static bool IosContains(UIKit.UIView ancestor, UIKit.UIView view)
    {
        for (var current = view; current is not null; current = current.Superview)
        {
            if (current == ancestor)
                return true;
        }

        return false;
    }
#endif

#if ANDROID
    void UnclipAndroid(Android.Views.View? start)
    {
        for (var current = start; current is not null; current = current.Parent as Android.Views.View)
        {
            if (!_seen.Add(current))
                continue;

            var clipBounds = current.ClipBounds;
            var clipToOutline = current.ClipToOutline;
            if (clipBounds is not null || clipToOutline)
            {
                current.ClipBounds = null;
                current.ClipToOutline = false;
                _restore.Add(() =>
                {
                    current.ClipBounds = clipBounds;
                    current.ClipToOutline = clipToOutline;
                });
            }

            if (current is not Android.Views.ViewGroup group)
                continue;

            var clipChildren = group.ClipChildren;
            var clipToPadding = group.ClipToPadding;
            if (!clipChildren && !clipToPadding)
                continue;

            group.SetClipChildren(false);
            group.SetClipToPadding(false);
            _restore.Add(() =>
            {
                group.SetClipChildren(clipChildren);
                group.SetClipToPadding(clipToPadding);
            });
        }
    }

    void HoldAndroidScrollSiblings(List<Android.Views.View> heroes)
    {
        var keep = new HashSet<Android.Views.View>();
        var scrolls = new List<Android.Views.View>();
        foreach (var hero in heroes)
        {
            for (var current = hero; current is not null; current = current.Parent as Android.Views.View)
                keep.Add(current);

            for (var current = hero.Parent as Android.Views.View; current is not null; current = current.Parent as Android.Views.View)
            {
                if (IsAndroidScroll(current) && !scrolls.Contains(current))
                    scrolls.Add(current);
            }
        }

        if (scrolls.Count == 0)
            return;

        var masked = new HashSet<Android.Views.View>();
        foreach (var hero in heroes)
        {
            Android.Views.View? top = null;
            for (var current = hero.Parent as Android.Views.View; current is not null; current = current.Parent as Android.Views.View)
            {
                if (IsAndroidScroll(current))
                    top = current;
            }

            for (var current = hero; current is not null && current != top; current = current.Parent as Android.Views.View)
            {
                if (current.Parent is not Android.Views.ViewGroup parent)
                    break;

                for (var index = 0; index < parent.ChildCount; index++)
                {
                    var sibling = parent.GetChildAt(index);
                    if (sibling is null || keep.Contains(sibling) || !masked.Add(sibling))
                        continue;

                    MaskAndroidSibling(sibling, parent, scrolls);
                }
            }
        }
    }

    void MaskAndroidSibling(Android.Views.View sibling, Android.Views.View parent, List<Android.Views.View> scrolls)
    {
        var left = int.MinValue;
        var top = int.MinValue;
        var right = int.MaxValue;
        var bottom = int.MaxValue;
        var any = false;
        var siblingLocation = new int[2];
        sibling.GetLocationOnScreen(siblingLocation);

        foreach (var scroll in scrolls)
        {
            if (!AndroidContains(scroll, parent))
                continue;

            var scrollLocation = new int[2];
            scroll.GetLocationOnScreen(scrollLocation);
            var clipLeft = scrollLocation[0] - siblingLocation[0];
            var clipTop = scrollLocation[1] - siblingLocation[1];
            left = Math.Max(left, clipLeft);
            top = Math.Max(top, clipTop);
            right = Math.Min(right, clipLeft + scroll.Width);
            bottom = Math.Min(bottom, clipTop + scroll.Height);
            any = true;
        }

        if (!any)
            return;

        if (right < left)
            right = left;
        if (bottom < top)
            bottom = top;

        var previous = sibling.ClipBounds;
        sibling.ClipBounds = new Android.Graphics.Rect(left, top, right, bottom);
        _restore.Add(() => sibling.ClipBounds = previous);
    }

    static bool IsAndroidScroll(Android.Views.View view)
    {
        for (var type = view.GetType(); type is not null; type = type.BaseType)
        {
            var name = type.Name;
            if (name.Contains("ScrollView", StringComparison.Ordinal)
                || name.Contains("RecyclerView", StringComparison.Ordinal)
                || name.Contains("AbsListView", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    static bool AndroidContains(Android.Views.View ancestor, Android.Views.View view)
    {
        for (var current = view; current is not null; current = current.Parent as Android.Views.View)
        {
            if (current == ancestor)
                return true;
        }

        return false;
    }
#endif

#if WINDOWS
    void UnclipWindows(Microsoft.UI.Xaml.UIElement? start)
    {
        for (var current = start; current is not null; current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current) as Microsoft.UI.Xaml.UIElement)
        {
            if (!_seen.Add(current))
                continue;

            var clip = current.Clip;
            if (clip is null)
                continue;

            current.Clip = null;
            _restore.Add(() => current.Clip = clip);
        }
    }
#endif
}
