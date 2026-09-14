using System.ComponentModel;
using MauiPage = Microsoft.Maui.Controls.Page;

namespace Reactor.Animate;

static class PageCover
{
    static readonly HashSet<MauiPage> Concealed = [];
    static readonly object Gate = new();

    public static void Conceal(MauiPage page)
    {
        lock (Gate)
            Concealed.Add(page);

        page.HandlerChanged -= OnHandlerChanged;
        page.HandlerChanged += OnHandlerChanged;
        page.Loaded -= OnLoaded;
        page.Loaded += OnLoaded;
        page.PropertyChanged -= OnPropertyChanged;
        page.PropertyChanged += OnPropertyChanged;
        ApplyHide(page);
    }

    public static void Reveal(MauiPage page)
    {
        lock (Gate)
            Concealed.Remove(page);

        page.HandlerChanged -= OnHandlerChanged;
        page.Loaded -= OnLoaded;
        page.PropertyChanged -= OnPropertyChanged;
        ApplyShow(page);
    }

    static bool IsConcealed(MauiPage page)
    {
        lock (Gate)
            return Concealed.Contains(page);
    }

    static void OnHandlerChanged(object? sender, EventArgs e)
    {
        if (sender is MauiPage page && IsConcealed(page))
            ApplyHide(page);
    }

    static void OnLoaded(object? sender, EventArgs e)
    {
        if (sender is MauiPage page && IsConcealed(page))
            ApplyHide(page);
    }

    static void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is MauiPage page
            && e.PropertyName == nameof(VisualElement.Opacity)
            && IsConcealed(page)
            && page.Opacity != 0)
            ApplyHide(page);
    }

    static void ApplyHide(MauiPage page)
    {
        page.Opacity = 0;
        page.Handler?.UpdateValue(nameof(VisualElement.Opacity));
        SetPlatformOpacity(page, 0);
    }

    static void ApplyShow(MauiPage page)
    {
        page.Opacity = 1;
        page.Handler?.UpdateValue(nameof(VisualElement.Opacity));
        SetPlatformOpacity(page, 1);
    }

    static void SetPlatformOpacity(MauiPage page, double opacity)
    {
#if IOS || MACCATALYST
        if (page.Handler?.PlatformView is UIKit.UIView view)
            view.Alpha = (nfloat)opacity;
#elif ANDROID
        if (page.Handler?.PlatformView is Android.Views.View view)
            view.Alpha = (float)opacity;
#elif WINDOWS
        if (page.Handler?.PlatformView is Microsoft.UI.Xaml.UIElement element)
            element.Opacity = opacity;
#endif
    }
}
