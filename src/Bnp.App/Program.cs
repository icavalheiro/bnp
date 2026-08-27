using Avalonia;
using Bnp.Diagnostics;

namespace Bnp;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        StartupMetrics.Start();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect();
    }
}