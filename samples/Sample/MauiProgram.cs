using MauiReactor;
using MauiReactor.HotReload;
using Sample.Components;
using Sample.Resources.Styles;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;

namespace Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiReactorApp<App>(app =>
                {
                    app.UseTheme<ApplicationTheme>();
                },
                unhandledExceptionAction: e =>
                {
                    System.Diagnostics.Debug.WriteLine(e.ExceptionObject);
                })
#if DEBUG
            .UseMauiReactorHotReload()
#endif
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        return builder.Build();
    }
}
