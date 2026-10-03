using Reactor.Animate;
using Microsoft.Maui.Platform;

namespace Reactor.Animate.Internals;

/// <summary>
/// Lets flying heroes paint outside CollectionView cells and other clipping
/// parents for the clip. Restored on dispose.
/// </summary>
internal sealed class FlightOverflow : IDisposable
{
    const int FlightZIndex = 10_000;

    readonly List<Action> _restore = [];
    readonly HashSet<object> _seen = [];
    bool _disposed;

    public FlightOverflow(IEnumerable<VisualElement> heroes)
    {
        foreach (var hero in heroes)
        {
            Raise(hero);
            UnclipLayouts(hero);
            UnclipNative(hero);
        }
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
