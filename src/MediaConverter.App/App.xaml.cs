using System;
using System.Windows;
using MediaConverter.Core.Interfaces;
using MediaConverter.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MediaConverter.App;

/// <summary>
/// WPF application entry point. Builds the dependency injection container that backs the
/// Blazor components, hosts the main window and disposes the container (and therefore all
/// disposable singletons) when the application shuts down.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    /// <inheritdoc/>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _serviceProvider = BuildServiceProvider();

        var mainWindow = new MainWindow(_serviceProvider);
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    /// <inheritdoc/>
    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    /// <summary>
    /// Registers the Core services. Only <see cref="IBinariesProvisioningService"/> is used by
    /// the UI in this phase; the audio and video services are registered now so later phases can
    /// inject them directly. <see cref="BinariesProvisioningService"/> is a singleton because it
    /// owns a long-lived <see cref="System.Net.Http.HttpClient"/> that must be reused and disposed.
    /// </summary>
    /// <returns>The configured <see cref="ServiceProvider"/>.</returns>
    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IBinariesProvisioningService, BinariesProvisioningService>();
        services.AddSingleton<IAudioConverterService, AudioConverterService>();
        services.AddSingleton<IVideoDownloaderService, VideoDownloaderService>();

        return services.BuildServiceProvider();
    }
}
