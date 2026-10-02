using Reactor.Animate;

namespace Reactor.Animate.Internals;

/// <summary>
/// Stops the platform from popping with its own animation. iOS/Mac Catalyst:
/// disable the interactive edge-swipe. Android: take system back (button or
/// predictive gesture) and route it through <see cref="Animate.Page.PopAsync"/>.
/// </summary>
internal static class PlatformPop
{
    static Microsoft.Maui.Controls.NavigationPage? _page;
#if ANDROID
    static AndroidX.Activity.OnBackPressedCallback? _callback;
#endif

    public static void Attach(Microsoft.Maui.Controls.NavigationPage? page)
    {
        if (ReferenceEquals(_page, page))
            return;

        Detach();
        _page = page;
        if (page is null)
            return;

        page.HandlerChanged += OnHandlerChanged;
        page.Pushed += OnNavigated;
        page.Popped += OnNavigated;
        page.PoppedToRoot += OnNavigated;
        Apply(page);
    }

    public static void Detach()
    {
        if (_page is null)
            return;

        _page.HandlerChanged -= OnHandlerChanged;
        _page.Pushed -= OnNavigated;
        _page.Popped -= OnNavigated;
        _page.PoppedToRoot -= OnNavigated;
        _page = null;
#if ANDROID
        _callback?.Remove();
        _callback = null;
#endif
    }

    static void OnHandlerChanged(object? sender, EventArgs e)
        => ApplyFrom(sender);

    static void OnNavigated(object? sender, NavigationEventArgs e)
        => ApplyFrom(sender);

    static void ApplyFrom(object? sender)
    {
        if (sender is Microsoft.Maui.Controls.NavigationPage page)
            Apply(page);
    }

    static void Apply(Microsoft.Maui.Controls.NavigationPage page)
    {
#if IOS || MACCATALYST
        if (page.Handler?.PlatformView is UIKit.UINavigationController controller
            && controller.InteractivePopGestureRecognizer is { } gesture)
        {
            gesture.Enabled = false;
        }
#endif
#if ANDROID
        BindAndroidBack(page);
#endif
    }

#if ANDROID
    static void BindAndroidBack(Microsoft.Maui.Controls.NavigationPage page)
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity
            as AndroidX.Activity.ComponentActivity;
        if (activity is null)
            return;

        if (_callback is null)
        {
            _callback = new BackCallback();
            activity.OnBackPressedDispatcher.AddCallback(activity, _callback);
        }

        _callback.Enabled = page.Navigation.NavigationStack.Count > 1;
    }

    sealed class BackCallback() : AndroidX.Activity.OnBackPressedCallback(true)
    {
        public override void HandleOnBackPressed()
        {
            var navigation = HostContext.Current.Navigation;
            if (navigation is { NavigationStack.Count: > 1 })
            {
                _ = Animate.Page.PopAsync();
                return;
            }

            Enabled = false;
            try
            {
                if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity
                    is AndroidX.Activity.ComponentActivity activity)
                {
                    activity.OnBackPressedDispatcher.OnBackPressed();
                }
            }
            finally
            {
                Enabled = HostContext.Current.Navigation is { NavigationStack.Count: > 1 };
            }
        }
    }
#endif
}
