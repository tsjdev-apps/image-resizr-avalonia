using Avalonia;

namespace ImageResizr.App;

/// <summary>
/// Provides the desktop application entry point.
/// </summary>
internal sealed class Program
{
    /// <summary>
    /// Starts the Avalonia desktop application.
    /// </summary>
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// Builds the configured Avalonia application builder.
    /// </summary>
    /// <returns>The configured Avalonia application builder.</returns>
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
