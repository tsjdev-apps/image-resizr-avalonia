using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ImageResizr.App.ViewModels;
using ImageResizr.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ImageResizr.App;

/// <summary>
/// Configures application services and desktop lifetime behavior.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? serviceProvider;

    /// <summary>
    /// Loads the Avalonia application markup.
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Creates the main window and completes desktop lifetime initialization.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            serviceProvider = ConfigureServices();
            desktop.MainWindow = serviceProvider.GetRequiredService<MainWindow>();
            desktop.Exit += DesktopOnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Configures the application service provider.
    /// </summary>
    /// <returns>The configured application service provider.</returns>
    private static ServiceProvider ConfigureServices()
    {
        ServiceCollection services = new();

        _ = services.AddSingleton<IImageResizrService, ImageResizrService>();
        _ = services.AddSingleton<MainWindowViewModel>();
        _ = services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }

    /// <summary>
    /// Disposes the application service provider when the desktop lifetime exits.
    /// </summary>
    private void DesktopOnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        serviceProvider?.Dispose();
        serviceProvider = null;
    }
}
