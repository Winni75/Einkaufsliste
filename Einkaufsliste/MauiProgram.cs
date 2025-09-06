using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

namespace Einkaufsliste
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                // Android: Status-/Navigationsleiste einfärben (modern, ohne veraltete APIs)
                .ConfigureLifecycleEvents(events =>
                {
#if ANDROID
                    events.AddAndroid(android =>
                    {
                        android.OnCreate((activity, _) => SetSystemBars(activity));
                        android.OnResume(activity => SetSystemBars(activity));
                    });
#endif
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

#if ANDROID
        // Grün für Status-/Navigationsleiste
        private const string BarColorHex = "#2E7D32";

        static void SetSystemBars(Android.App.Activity activity)
        {
            var window = activity?.Window;
            if (window is null) return;

            var color = Android.Graphics.Color.ParseColor(BarColorHex);

            // Farben setzen
            window.SetStatusBarColor(color);
            window.SetNavigationBarColor(color);

            // Icons steuern über AndroidX WindowInsetsController (empfohlen)
            // -> false = helle (weiße) Icons; true = dunkle Icons
            var controller = AndroidX.Core.View.WindowCompat.GetInsetsController(window, window.DecorView);
            if (controller is not null)
            {
                controller.AppearanceLightStatusBars = false;      // weiße Statusbar-Icons
                controller.AppearanceLightNavigationBars = false;  // weiße Nav-Bar-Icons
            }
        }
#endif
    }
}
