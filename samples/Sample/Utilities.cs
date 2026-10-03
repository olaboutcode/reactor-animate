namespace Sample.Components;

internal static class CustomColors
{
    public static Color Gray600 { get; } = Color.FromRgba(64, 64, 64, 255); // #404040
    public static Color Gray100 { get; } = Color.FromRgba(245, 245, 245, 255); // #F5F5F5
    public static Color Gray300 { get; } = Color.FromRgba(172, 172, 172, 255); // #ACACAC
}

internal static class Radius
{
    public const double None = 0;
    public const double XSmall = 4;
    public const double Small = 8;
    public const double Medium = 12;
    public const double Large = 16;
    public const double XLarge = 24;
    public const double Full = 9999;
}

internal static class TouchTarget
{
    public const double Min = 44;
    public const double Comfortable = 48;
    public const double Large = 56;
}

internal static class IconSizing
{
    public const double XSmall = 16;
    public const double Small = 20;
    public const double Medium = 24;
    public const double Large = 28;
    public const double XLarge = 44;
}

internal static class Spacing
{
    public const double XXSmall = 2;
    public const double XSmall = 4;
    public const double Small = 8;
    public const double Medium = 16;
    public const double Large = 24;
    public const double XLarge = 32;
    public const double XXLarge = 48;
}

internal static class FontSizing
{
    public const double Caption = 11;
    public const double Label = 12;
    public const double Body = 14;
    public const double BodyLarge = 15;
    public const double Subtitle = 16;
    public const double Title = 18;
    public const double Heading = 20;
    public const double Display = 28;
}

internal static class ViewExtensions
{
    public static MauiReactor.ContentPage HideNavigationBar(this MauiReactor.ContentPage contentPage, bool hide = true)
        => contentPage
            .Set(MauiControls.Shell.NavBarIsVisibleProperty, !hide)
            .HasNavigationBar(!hide);
}

/// <summary>
/// Starts a page flight from a synchronous click handler.
/// The handler is <see cref="Action"/>, so the task is observed here.
/// </summary>
internal static class PageNavigation
{
    public static void Push<TPage>()
        where TPage : Component, new()
        => Run(() => Animate.Page.PushAsync<TPage>());

    public static void Pop()
        => Run(() => Animate.Page.PopAsync());

    public static void Run(Func<Task> navigate)
    {
        _ = Observe(navigate());

        static async Task Observe(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
    }
}
